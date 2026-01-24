using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sistema_Almacen.Data;
using Sistema_Almacen.Models.ViewModels;

namespace Sistema_Almacen.Controllers
{
    /// <summary>
    /// Controlador del Dashboard principal
    /// Solo accesible para usuarios autenticados
    /// </summary>
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var hoy = DateTime.Today;

            // Consultas simples que SQLite sí soporta (Count)
            var totalProductos = await _context.Productos.CountAsync();
            var productosStockBajo = await _context.LotesInventario
                    .GroupBy(l => l.ProductoId)
                    .Select(g => g.Sum(l => l.StockActual))
                    .CountAsync(stock => stock < 10);

            // SQLite tiene problemas con SumAsync en decimales, lo calculamos en memoria (ToList)
            var lotesData = await _context.LotesInventario
                .Select(l => new { l.StockActual, l.CostoUnitario })
                .ToListAsync();
            var valorInventario = lotesData.Sum(l => (decimal)l.StockActual * l.CostoUnitario);

            var ventasHoyData = await _context.Ventas
                .Where(v => v.Fecha.Date == hoy)
                .Select(v => v.Total)
                .ToListAsync();
            var ventasHoyCount = ventasHoyData.Count;
            var ingresosHoy = ventasHoyData.Sum();

            var productosRecientes = await _context.Productos
                    .OrderByDescending(p => p.Id)
                    .Take(5)
                    .ToListAsync();

            var model = new DashboardViewModel
            {
                TotalProductos = totalProductos,
                ProductosStockBajo = productosStockBajo,
                ValorInventario = valorInventario,
                VentasHoy = ventasHoyCount,
                IngresosHoy = ingresosHoy,
                ProductosRecientes = productosRecientes
            };

            // Pasar información adicional del usuario mediante ViewBag
            ViewBag.NombreUsuario = User.Identity?.Name;
            ViewBag.Rol = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Role)?.Value;
            
            return View(model);
        }
    }
}

