using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sistema_Almacen.Data;
using Sistema_Almacen.Models.ViewModels;

namespace Sistema_Almacen.Controllers
{

    public class InventarioController : Controller
    {
        private readonly ApplicationDbContext _context;

        public InventarioController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(int? sucursalId)
        {
            // Obtener sucursales para el filtro
            ViewBag.Sucursales = await _context.Sucursales.ToListAsync();
            ViewBag.SucursalSeleccionada = sucursalId;

            // Consultar lotes con stock actual > 0
            var query = _context.LotesInventario
                .Include(l => l.Producto)
                .Include(l => l.Sucursal)
                .Where(l => l.StockActual > 0);

            if (sucursalId.HasValue && sucursalId > 0)
            {
                query = query.Where(l => l.SucursalId == sucursalId.Value);
            }

            // Agrupar por producto y sucursal para mostrar el stock total usando el ViewModel
            var inventario = await query
                .GroupBy(l => new { l.ProductoId, ProductoNombre = l.Producto.Nombre, l.Producto.SKU, SucursalNombre = l.Sucursal.Nombre, l.SucursalId })
                .Select(g => new InventarioViewModel
                {
                    Producto = g.Key.ProductoNombre,
                    SKU = g.Key.SKU,
                    Sucursal = g.Key.SucursalNombre,
                    SucursalId = g.Key.SucursalId,
                    StockTotal = g.Sum(l => l.StockActual)
                })
                .ToListAsync();

            return View(inventario);
        }
    }
}
