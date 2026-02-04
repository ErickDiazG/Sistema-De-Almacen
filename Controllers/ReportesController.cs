using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sistema_Almacen.Data;
using Sistema_Almacen.Models;

namespace Sistema_Almacen.Controllers
{

    public class ReportesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReportesController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // Recuperar movimientos incluyendo el usuario (responsable)
            var movimientos = await _context.MovimientosAlmacen
                .Include(m => m.Usuario)
                .OrderByDescending(m => m.Fecha)
                .ToListAsync();

            return View(movimientos);
        }

        public async Task<IActionResult> Auditoria()
        {
            // Por ahora mostramos lo mismo, pero con enfoque en control
            var movimientos = await _context.MovimientosAlmacen
                .Include(m => m.Usuario)
                .OrderByDescending(m => m.Fecha)
                .ToListAsync();
            
            return View("Index", movimientos);
        }

        public async Task<IActionResult> Graficos()
        {
            // 1. COMPARATIVA ENTRADAS VS SALIDAS (Últimos 6 meses)
            var fechaInicio = DateTime.Today.AddMonths(-6);
            var datosMovimientos = await _context.MovimientosAlmacen
                .Where(m => m.Fecha >= fechaInicio)
                .GroupBy(m => new { m.Fecha.Year, m.Fecha.Month })
                .Select(g => new
                {
                    Anio = g.Key.Year,
                    Mes = g.Key.Month,
                    Entradas = g.Where(m => m.Tipo == TipoMovimiento.Entrada).Sum(m => m.Cantidad),
                    Salidas = g.Where(m => m.Tipo == TipoMovimiento.Salida).Sum(m => m.Cantidad)
                })
                .OrderBy(x => x.Anio).ThenBy(x => x.Mes)
                .ToListAsync();

            // Formatear para Chart.js
            var etiquetasMeses = datosMovimientos.Select(x => new DateTime(x.Anio, x.Mes, 1).ToString("MMM yyyy")).ToArray();
            var dataEntradas = datosMovimientos.Select(x => x.Entradas).ToArray();
            var dataSalidas = datosMovimientos.Select(x => x.Salidas).ToArray();

            ViewBag.ChartLabels = System.Text.Json.JsonSerializer.Serialize(etiquetasMeses);
            ViewBag.ChartDataEntradas = System.Text.Json.JsonSerializer.Serialize(dataEntradas);
            ViewBag.ChartDataSalidas = System.Text.Json.JsonSerializer.Serialize(dataSalidas);

            // 2. TOP 5 PRODUCTOS MÁS VENDIDOS (Basado en DetallesVenta)
            var topProductos = await _context.DetallesVenta
                .Include(d => d.Producto)
                .GroupBy(d => d.Producto.Nombre)
                .Select(g => new
                {
                    Producto = g.Key,
                    Cantidad = g.Sum(d => d.Cantidad)
                })
                .OrderByDescending(x => x.Cantidad)
                .Take(5)
                .ToListAsync();

            ViewBag.TopProductosLabels = System.Text.Json.JsonSerializer.Serialize(topProductos.Select(x => x.Producto));
            ViewBag.TopProductosData = System.Text.Json.JsonSerializer.Serialize(topProductos.Select(x => x.Cantidad));

            // 3. VALOR TOTAL DEL INVENTARIO
            // SQLite no soporta Sum(decimal * int) directamente en EF Core 8.0, traemos datos a memoria
            var lotes = await _context.LotesInventario
                .Select(l => new { l.StockActual, l.CostoUnitario })
                .ToListAsync();
                
            var valorTotal = lotes.Sum(l => l.StockActual * l.CostoUnitario);
            
            ViewBag.ValorInventario = valorTotal;

            return View();
        }
    }
}
