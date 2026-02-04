using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Sistema_Almacen.Data;
using Sistema_Almacen.Models;
using Sistema_Almacen.Services;
using System.Security.Claims;

namespace Sistema_Almacen.Controllers
{

    public class PrestamosController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IInventarioService _inventarioService;

        public PrestamosController(ApplicationDbContext context, IInventarioService inventarioService)
        {
            _context = context;
            _inventarioService = inventarioService;
        }

        /// <summary>
        /// GET: Lista de préstamos con filtros y alertas visuales
        /// </summary>
        public async Task<IActionResult> Index(string filtro = "activos")
        {
            ViewBag.FiltroActual = filtro;

            var query = _context.Prestamos
                .Include(p => p.Producto)
                .Include(p => p.UsuarioRegistro)
                .AsQueryable();

            // Aplicar filtros
            switch (filtro.ToLower())
            {
                case "activos":
                    query = query.Where(p => p.Estatus == EstatusPrestamo.Activo);
                    break;
                case "atrasados":
                    query = query.Where(p => p.Estatus == EstatusPrestamo.Activo && p.FechaEsperadaRegreso < DateTime.Now);
                    break;
                case "devueltos":
                    query = query.Where(p => p.Estatus == EstatusPrestamo.Devuelto);
                    break;
                case "todos":
                default:
                    // No filtrar
                    break;
            }

            var prestamos = await query
                .OrderByDescending(p => p.Estatus == EstatusPrestamo.Activo) // Activos primero
                .ThenBy(p => p.FechaEsperadaRegreso) // Más urgentes primero
                .ToListAsync();

            // Estadísticas para el dashboard
            ViewBag.TotalActivos = await _context.Prestamos.CountAsync(p => p.Estatus == EstatusPrestamo.Activo);
            ViewBag.TotalAtrasados = await _context.Prestamos.CountAsync(p => p.Estatus == EstatusPrestamo.Activo && p.FechaEsperadaRegreso < DateTime.Now);
            ViewBag.TotalDevueltos = await _context.Prestamos.CountAsync(p => p.Estatus == EstatusPrestamo.Devuelto);

            return View(prestamos);
        }

        /// <summary>
        /// GET: Formulario para crear nuevo préstamo
        /// </summary>
        public async Task<IActionResult> Create()
        {
            await CargarCombos();
            return View();
        }

        /// <summary>
        /// POST: Registra un nuevo préstamo de activo
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Prestamo prestamo)
        {
            // Remover propiedades de navegación del ModelState
            ModelState.Remove("Producto");
            ModelState.Remove("UsuarioRegistro");

            if (!ModelState.IsValid)
            {
                await CargarCombos();
                return View(prestamo);
            }

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Verificar stock disponible usando el servicio
                var stockDisponible = await _inventarioService.ObtenerStockDisponible(prestamo.ProductoId, null); // null sucursal?? Prestamos no parece filtrar por sucursal en el original?
                // Revisando el código original:
                // var stockDisponible = await _context.LotesInventario
                //    .Where(l => l.ProductoId == prestamo.ProductoId && l.StockActual > 0)
                //    .SumAsync(l => l.StockActual);
                // No filtraba por sucursal. Así que enviamos null.

                if (stockDisponible < prestamo.Cantidad)
                {
                    ModelState.AddModelError("Cantidad", $"Stock insuficiente. Disponible: {stockDisponible}");
                    await CargarCombos();
                    return View(prestamo);
                }

                // Configurar fechas
                prestamo.FechaSalida = DateTime.Now;
                
                // Si no se especificó fecha de regreso, usar 7 días por defecto
                if (prestamo.FechaEsperadaRegreso == default)
                {
                    prestamo.FechaEsperadaRegreso = DateTime.Now.AddDays(7);
                }

                prestamo.Estatus = EstatusPrestamo.Activo;

                // Obtener ID del usuario actual
                // MODIFICADO: Sistema sin login, usar ID de Admin (1) por defecto
                var userIdClaim = "1"; 
                if (userIdClaim != null)
                {
                    prestamo.UsuarioRegistroId = int.Parse(userIdClaim);
                }

                // Descontar stock temporalmente usando FIFO (Servicio)
                // Nota: El original no filtraba por sucursal, así que enviamos null
                await _inventarioService.DescontarStockFIFO(prestamo.ProductoId, prestamo.Cantidad, null);

                // Registrar movimiento de auditoría
                var movimiento = new MovimientoAlmacen
                {
                    Fecha = DateTime.Now,
                    UsuarioId = prestamo.UsuarioRegistroId ?? 1,
                    Tipo = TipoMovimiento.Salida,
                    Cantidad = prestamo.Cantidad,
                    Referencia = $"Préstamo a {prestamo.UsuarioSolicitante} - {prestamo.Producto?.Nombre ?? "Activo"}"
                };

                _context.MovimientosAlmacen.Add(movimiento);

                // Guardar préstamo
                _context.Prestamos.Add(prestamo);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["Success"] = $"Préstamo registrado exitosamente. Fecha de devolución esperada: {prestamo.FechaEsperadaRegreso:dd/MM/yyyy}";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                ModelState.AddModelError("", $"Error al procesar el préstamo: {ex.Message}");
            }

            await CargarCombos();
            return View(prestamo);
        }

        /// <summary>
        /// POST: Registra la devolución de un préstamo
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Devolver(int id, string? comentarioDevolucion)
        {
            var prestamo = await _context.Prestamos
                .Include(p => p.Producto)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (prestamo == null)
            {
                return Json(new { success = false, message = "Préstamo no encontrado." });
            }

            if (prestamo.Estatus == EstatusPrestamo.Devuelto)
            {
                return Json(new { success = false, message = "Este préstamo ya fue devuelto." });
            }

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Marcar como devuelto
                prestamo.Estatus = EstatusPrestamo.Devuelto;
                prestamo.FechaDevolucionReal = DateTime.Now;
                
                if (!string.IsNullOrWhiteSpace(comentarioDevolucion))
                {
                    prestamo.Comentarios = string.IsNullOrEmpty(prestamo.Comentarios) 
                        ? $"Devolución: {comentarioDevolucion}"
                        : $"{prestamo.Comentarios} | Devolución: {comentarioDevolucion}";
                }

                // Liberar stock: crear nuevo lote con la cantidad devuelta
                var ubicacionPorDefecto = await _context.Ubicaciones.FirstOrDefaultAsync();
                var sucursalPorDefecto = await _context.Sucursales.FirstOrDefaultAsync();

                if (ubicacionPorDefecto != null && sucursalPorDefecto != null)
                {
                    var nuevoLote = new LoteInventario
                    {
                        ProductoId = prestamo.ProductoId,
                        UbicacionId = ubicacionPorDefecto.Id,
                        SucursalId = sucursalPorDefecto.Id,
                        CantidadInicial = prestamo.Cantidad,
                        StockActual = prestamo.Cantidad,
                        CostoUnitario = 0, // Devolución, no tiene costo nuevo
                        FechaEntrada = DateTime.Now
                    };

                    _context.LotesInventario.Add(nuevoLote);
                }

                // Registrar movimiento de entrada
                // MODIFICADO: Sistema sin login, usar ID de Admin (1) por defecto
                var userIdClaim = "1"; 
                int usuarioId = userIdClaim != null ? int.Parse(userIdClaim) : 1;

                var movimiento = new MovimientoAlmacen
                {
                    Fecha = DateTime.Now,
                    UsuarioId = usuarioId,
                    Tipo = TipoMovimiento.Entrada,
                    Cantidad = prestamo.Cantidad,
                    Referencia = $"Devolución de préstamo #{prestamo.Id} - {prestamo.UsuarioSolicitante}"
                };

                _context.MovimientosAlmacen.Add(movimiento);

                _context.Prestamos.Update(prestamo);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // Calcular si estaba atrasado
                bool estabaAtrasado = prestamo.FechaDevolucionReal > prestamo.FechaEsperadaRegreso;
                int diasAtraso = estabaAtrasado 
                    ? (int)(prestamo.FechaDevolucionReal.Value - prestamo.FechaEsperadaRegreso).TotalDays 
                    : 0;

                return Json(new
                {
                    success = true,
                    message = estabaAtrasado 
                        ? $"Devolución registrada. Nota: El préstamo estaba atrasado por {diasAtraso} día(s)."
                        : "Devolución registrada exitosamente. ¡A tiempo!",
                    estabaAtrasado,
                    diasAtraso
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return Json(new { success = false, message = $"Error al procesar la devolución: {ex.Message}" });
            }
        }

        /// <summary>
        /// GET: Detalles de un préstamo específico
        /// </summary>
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var prestamo = await _context.Prestamos
                .Include(p => p.Producto)
                .Include(p => p.UsuarioRegistro)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (prestamo == null) return NotFound();

            return View(prestamo);
        }

        /// <summary>
        /// Carga los combos para el formulario
        /// </summary>
        private async Task CargarCombos()
        {
            // Solo mostrar productos que son activos fijos Y tienen stock
            var activosFijos = await _context.Productos
                .Where(p => p.EsActivoFijo)
                .Select(p => new
                {
                    p.Id,
                    Display = $"{p.SKU} - {p.Nombre}",
                    StockDisponible = _context.LotesInventario
                        .Where(l => l.ProductoId == p.Id && l.StockActual > 0)
                        .Sum(l => l.StockActual)
                })
                .Where(p => p.StockDisponible > 0)
                .ToListAsync();

            if (!activosFijos.Any())
            {
                // Si no hay activos fijos, mostrar todos los productos con stock
                var todosProductos = await _context.Productos
                    .Select(p => new
                    {
                        p.Id,
                        Display = $"{p.SKU} - {p.Nombre}",
                        StockDisponible = _context.LotesInventario
                            .Where(l => l.ProductoId == p.Id && l.StockActual > 0)
                            .Sum(l => l.StockActual)
                    })
                    .Where(p => p.StockDisponible > 0)
                    .ToListAsync();

                ViewData["ProductoId"] = new SelectList(todosProductos, "Id", "Display");
                ViewBag.MostrarTodos = true;
            }
            else
            {
                ViewData["ProductoId"] = new SelectList(activosFijos, "Id", "Display");
                ViewBag.MostrarTodos = false;
            }
        }
    }
}
