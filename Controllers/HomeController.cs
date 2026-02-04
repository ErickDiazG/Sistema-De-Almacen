using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Sistema_Almacen.Data;
using Sistema_Almacen.Models;
using Sistema_Almacen.Models.DTOs;
using System.Diagnostics;
using System.Security.Claims;

namespace Sistema_Almacen.Controllers
{

    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<HomeController> _logger;

        public HomeController(ApplicationDbContext context, ILogger<HomeController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// GET: Muestra la interfaz del Carrito de Requisiciones
        /// </summary>
        public async Task<IActionResult> Index()
        {
            await CargarCombos();
            return View();
        }

        /// <summary>
        /// GET: Obtiene productos con stock disponible para el dropdown
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> ObtenerProductosConStock(int sucursalId)
        {
            var productosConStock = await _context.LotesInventario
                .Where(l => l.StockActual > 0 && l.SucursalId == sucursalId)
                .GroupBy(l => new { l.ProductoId, l.Producto.Nombre, l.Producto.SKU })
                .Select(g => new
                {
                    Id = g.Key.ProductoId,
                    Nombre = g.Key.Nombre,
                    SKU = g.Key.SKU,
                    StockDisponible = g.Sum(l => l.StockActual)
                })
                .OrderBy(p => p.Nombre)
                .ToListAsync();

            return Json(productosConStock);
        }

        /// <summary>
        /// GET: Verifica el stock disponible de un producto en una sucursal
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> VerificarStock(int productoId, int sucursalId)
        {
            var stockDisponible = await _context.LotesInventario
                .Where(l => l.ProductoId == productoId && l.SucursalId == sucursalId && l.StockActual > 0)
                .SumAsync(l => l.StockActual);

            var producto = await _context.Productos.FindAsync(productoId);

            return Json(new
            {
                productoId,
                productoNombre = producto?.Nombre,
                sku = producto?.SKU,
                stockDisponible
            });
        }

        /// <summary>
        /// POST: Procesa la requisición completa (salida múltiple FIFO)
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> ProcesarRequisicion([FromBody] RequisicionDto requisicion)
        {
            if (requisicion == null || requisicion.Items == null || requisicion.Items.Count == 0)
            {
                return Json(new { success = false, message = "La lista de requisición está vacía." });
            }

            // MODIFICADO: Sistema sin login, usar ID de Admin (1) por defecto
            var userIdClaim = "1"; // User.FindFirst(ClaimTypes.NameIdentifier);
            int usuarioId = 1; // Default
            
            if (userIdClaim != null)
            {
                usuarioId = int.Parse(userIdClaim);
            }

            var errores = new List<string>();
            var itemsProcesados = new List<string>();

            // Usar transacción para garantizar atomicidad
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                foreach (var item in requisicion.Items)
                {
                    // Verificar stock disponible total
                    var stockTotal = await _context.LotesInventario
                        .Where(l => l.ProductoId == item.ProductoId && l.SucursalId == item.SucursalId && l.StockActual > 0)
                        .SumAsync(l => l.StockActual);

                    if (stockTotal < item.Cantidad)
                    {
                        var producto = await _context.Productos.FindAsync(item.ProductoId);
                        errores.Add($"Stock insuficiente para '{producto?.Nombre}'. Disponible: {stockTotal}, Solicitado: {item.Cantidad}");
                        continue;
                    }

                    // Aplicar lógica FIFO: consumir de los lotes más antiguos primero
                    var cantidadPendiente = item.Cantidad;
                    var lotes = await _context.LotesInventario
                        .Where(l => l.ProductoId == item.ProductoId && l.SucursalId == item.SucursalId && l.StockActual > 0)
                        .OrderBy(l => l.FechaEntrada) // FIFO: primero los más antiguos
                        .ToListAsync();

                    foreach (var lote in lotes)
                    {
                        if (cantidadPendiente <= 0) break;

                        int cantidadARestar = Math.Min(lote.StockActual, cantidadPendiente);
                        lote.StockActual -= cantidadARestar;
                        cantidadPendiente -= cantidadARestar;

                        _context.LotesInventario.Update(lote);
                    }

                    // Registrar movimiento de salida
                    var movimiento = new MovimientoAlmacen
                    {
                        Fecha = DateTime.Now,
                        UsuarioId = usuarioId,
                        Tipo = TipoMovimiento.Salida,
                        Cantidad = item.Cantidad,
                        Referencia = string.IsNullOrWhiteSpace(requisicion.Referencia)
                            ? $"Requisición - {item.ProductoNombre}"
                            : $"{requisicion.Referencia} - {item.ProductoNombre}"
                    };

                    _context.MovimientosAlmacen.Add(movimiento);
                    itemsProcesados.Add($"{item.ProductoNombre} x{item.Cantidad}");
                }

                // Si hay errores, hacer rollback
                if (errores.Count > 0)
                {
                    await transaction.RollbackAsync();
                    return Json(new
                    {
                        success = false,
                        message = "No se pudo procesar la requisición por falta de stock.",
                        errores
                    });
                }

                // Guardar cambios y confirmar transacción
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation($"Requisición procesada: {itemsProcesados.Count} items por usuario {usuarioId}");

                return Json(new
                {
                    success = true,
                    message = $"Requisición procesada exitosamente. {itemsProcesados.Count} productos despachados.",
                    itemsProcesados,
                    referencia = requisicion.Referencia
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error al procesar requisición");

                return Json(new
                {
                    success = false,
                    message = "Error interno al procesar la requisición. Por favor intente nuevamente.",
                    error = ex.Message
                });
            }
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        private async Task CargarCombos()
        {
            ViewData["SucursalId"] = new SelectList(await _context.Sucursales.ToListAsync(), "Id", "Nombre");
            
            // Productos con stock agrupados (para carga inicial)
            var productosConStock = await _context.LotesInventario
                .Where(l => l.StockActual > 0)
                .Include(l => l.Producto)
                .GroupBy(l => new { l.ProductoId, l.Producto.Nombre, l.Producto.SKU })
                .Select(g => new
                {
                    Id = g.Key.ProductoId,
                    Display = $"{g.Key.SKU} - {g.Key.Nombre} (Stock: {g.Sum(l => l.StockActual)})"
                })
                .ToListAsync();

            ViewData["ProductoId"] = new SelectList(productosConStock, "Id", "Display");
        }
    }
}
