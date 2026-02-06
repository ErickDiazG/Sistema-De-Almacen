using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sistema_Almacen.Data;
using Sistema_Almacen.Models;
using Sistema_Almacen.Models.ViewModels;

namespace Sistema_Almacen.Controllers
{
    public class SalidasController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SalidasController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            ViewBag.Usuarios = await _context.Usuarios.OrderBy(u => u.Nombre).ToListAsync();
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> BuscarProducto(string term)
        {
            if (string.IsNullOrWhiteSpace(term)) return Json(null);

            // Buscar por SKU exacto (para lector de código) o Nombre (búsqueda parcial)
            var producto = await _context.Productos
                .Include(p => p.Lotes)
                .Where(p => p.SKU == term || p.Nombre.Contains(term))
                .Select(p => new 
                {
                    p.Id,
                    p.SKU,
                    p.Nombre,
                    p.PrecioVenta,
                    p.EsPrestable,
                    StockTotal = p.Lotes.Sum(l => l.StockActual)
                })
                .FirstOrDefaultAsync(); // Priorizamos coincidencia exacta o el primero

            if (producto == null) return Json(null);

            return Json(producto);
        }

        [HttpPost]
        public async Task<IActionResult> ProcesarSalida([FromBody] SalidaRequestViewModel request)
        {
            if (request == null || !request.Items.Any())
            {
                return Json(new { success = false, message = "El carrito está vacío." });
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Usuario "Registrador" (fixed por ahora, luego de User.Identity)
                int adminId = 1;
                
                // Si es PRESTAMO, validamos el solicitante
                string nombreSolicitante = "No asignado";
                if (request.TipoSalida == "PRESTAMO" && request.UsuarioSolicitanteId.HasValue)
                {
                    var user = await _context.Usuarios.FindAsync(request.UsuarioSolicitanteId.Value);
                    nombreSolicitante = user?.Nombre ?? "Desconocido";
                }

                foreach (var item in request.Items)
                {
                    // 1. Validar Stock Total
                    var stockTotal = await _context.LotesInventario
                        .Where(l => l.ProductoId == item.ProductoId)
                        .SumAsync(l => l.StockActual);

                    if (stockTotal < item.Cantidad)
                    {
                        var prod = await _context.Productos.FindAsync(item.ProductoId); // Solo para el nombre
                        throw new Exception($"Stock insuficiente para {prod?.Nombre ?? "Producto"}. Disponible: {stockTotal}, Solicitado: {item.Cantidad}");
                    }

                    // 2. FIFO ALGORITHM
                    // Traer lotes con stock > 0, ordenados por Fecha de Entrada (Los más viejos primero)
                    var lotes = await _context.LotesInventario
                        .Where(l => l.ProductoId == item.ProductoId && l.StockActual > 0)
                        .OrderBy(l => l.FechaEntrada)
                        .ToListAsync();

                    int cantidadPendiente = item.Cantidad;

                    foreach (var lote in lotes)
                    {
                        if (cantidadPendiente <= 0) break;

                        if (lote.StockActual >= cantidadPendiente)
                        {
                            // Este lote cubre todo lo que falta
                            lote.StockActual -= cantidadPendiente;
                            cantidadPendiente = 0;
                        }
                        else
                        {
                            // Este lote se agota, tomamos todo y seguimos al siguiente
                            cantidadPendiente -= lote.StockActual;
                            lote.StockActual = 0;
                        }
                    }

                    // Usamos el Costo Promedio del producto para valorizar la salida (Kardex)
                    var productoInfo = await _context.Productos.FindAsync(item.ProductoId);
                    decimal costoSalida = productoInfo?.CostoPromedio ?? 0;

                    // 3. Registrar Movimiento (Salida) / Préstamo
                    if (request.TipoSalida == "CONSUMO")
                    {
                        // Salida Definitiva
                        _context.MovimientosAlmacen.Add(new MovimientoAlmacen
                        {
                            Fecha = DateTime.Now,
                            UsuarioId = adminId,
                            Tipo = TipoMovimiento.Salida,
                            Cantidad = item.Cantidad,
                            Referencia = $"Consumo: {request.Referencia}",
                            ProductoId = item.ProductoId,
                            CostoUnitario = costoSalida
                        });
                    }
                    else if (request.TipoSalida == "PRESTAMO")
                    {
                        // Crear Registro de Préstamo
                        _context.Prestamos.Add(new Prestamo
                        {
                            ProductoId = item.ProductoId,
                            UsuarioSolicitante = nombreSolicitante, 
                            FechaSalida = DateTime.Now,
                            FechaEsperadaRegreso = DateTime.Now.AddDays(3), // Regla de negocio solicitada
                            Cantidad = item.Cantidad,
                            Estatus = EstatusPrestamo.Activo,
                            Comentarios = request.Referencia,
                            UsuarioRegistroId = adminId
                        });

                        // También registramos el movimiento de salida física
                        _context.MovimientosAlmacen.Add(new MovimientoAlmacen
                        {
                            Fecha = DateTime.Now,
                            UsuarioId = adminId,
                            Tipo = TipoMovimiento.Salida, 
                            Cantidad = item.Cantidad,
                            Referencia = $"Salida por Préstamo a {nombreSolicitante}",
                            ProductoId = item.ProductoId,
                            CostoUnitario = costoSalida
                        });
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Json(new { success = true, message = "Salida procesada correctamente." });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }
    }
}
