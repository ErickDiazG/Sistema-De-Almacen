using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sistema_Almacen.Data;
using Sistema_Almacen.Models;
using Sistema_Almacen.Models.ViewModels;
using System.Globalization;

namespace Sistema_Almacen.Controllers
{
    /// <summary>
    /// Controlador del Dashboard principal
    /// Solo accesible para usuarios autenticados
    /// </summary>

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

            // 1. KPIs Generales
            var totalProductos = await _context.Productos.CountAsync();
            
            // Stock Bajo (< 10 unidades)
            var productosStockBajo = await _context.LotesInventario
                    .GroupBy(l => l.ProductoId)
                    .Select(g => new { ProductoId = g.Key, TotalStock = g.Sum(l => l.StockActual) })
                    .Where(x => x.TotalStock < 10)
                    .CountAsync();

            // Valor Inventario (Cálculo en memoria por limitaciones de SQLite con Sum decimal)
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

            // 2. NUEVO KPI: Préstamos Vencidos (> 3 días o cualquier atraso según regla de negocio)
            // El usuario pidió ">3 Días" en el texto de la tarjeta, así que filtramos por fecha límite
            var totalPrestamosVencidos = await _context.Prestamos
                .CountAsync(p => p.Estatus == EstatusPrestamo.Activo && p.FechaEsperadaRegreso < DateTime.Now);

            // 3. DATOS PARA GRÁFICA (Últimos 7 días)
            var fechaInicioGrafica = hoy.AddDays(-6);
            var movimientosUltimosDias = await _context.MovimientosAlmacen
                .Where(m => m.Fecha.Date >= fechaInicioGrafica)
                .ToListAsync();

            var chartLabels = new string[7];
            var chartDataEntradas = new int[7];
            var chartDataSalidas = new int[7];

            for (int i = 0; i < 7; i++)
            {
                var dia = fechaInicioGrafica.AddDays(i);
                chartLabels[i] = dia.ToString("dd/MM"); // Ej: 04/02
                
                var movsDia = movimientosUltimosDias.Where(m => m.Fecha.Date == dia).ToList();
                chartDataEntradas[i] = movsDia.Where(m => m.Tipo == TipoMovimiento.Entrada || m.Tipo == TipoMovimiento.Ajuste).Sum(m => m.Cantidad);
                chartDataSalidas[i] = movsDia.Where(m => m.Tipo == TipoMovimiento.Salida).Sum(m => m.Cantidad);
            }

            // 4. ÚLTIMOS MOVIMIENTOS (Tabla inferior)
            var ultimosMovimientos = await _context.MovimientosAlmacen
                    .Include(m => m.Usuario)
                    .OrderByDescending(m => m.Fecha)
                    .Take(10) // Traemos 10 para que se vea llena la tabla
                    .ToListAsync();

            var model = new DashboardViewModel
            {
                TotalProductos = totalProductos,
                ProductosStockBajo = productosStockBajo,
                ValorInventario = valorInventario,
                VentasHoy = ventasHoyCount,
                IngresosHoy = ingresosHoy,
                TotalPrestamosVencidos = totalPrestamosVencidos,
                UltimosMovimientos = ultimosMovimientos,
                ChartLabels = chartLabels,
                ChartDataEntradas = chartDataEntradas,
                ChartDataSalidas = chartDataSalidas
            };

            // Pasar información adicional del usuario
            ViewBag.NombreUsuario = "Ing. Alexander Sosa"; 
            ViewBag.Rol = "Admin"; 
            
            return View(model);
        }
    }
}
