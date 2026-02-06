using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Sistema_Almacen.Data;
using Sistema_Almacen.Models;
using System.Text.Json;

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
            var movimientos = await _context.MovimientosAlmacen
                .Include(m => m.Usuario)
                .Include(m => m.Producto)
                .OrderByDescending(m => m.Fecha)
                .ToListAsync();

            return View(movimientos);
        }

        public async Task<IActionResult> Auditoria()
        {
            var movimientos = await _context.MovimientosAlmacen
                .Include(m => m.Usuario)
                .Include(m => m.Producto)
                .OrderByDescending(m => m.Fecha)
                .ToListAsync();

            return View("Index", movimientos);
        }

        // ============================================================
        // 1. THE KARDEX (Accounting Bible)
        // ============================================================
        public async Task<IActionResult> Kardex(int? productoId)
        {
            // Product Selector
            ViewData["ProductoId"] = new SelectList(
                await _context.Productos.OrderBy(p => p.Nombre).ToListAsync(), 
                "Id", 
                "Nombre", 
                productoId);

            if (!productoId.HasValue)
            {
                ViewBag.Kardex = new List<KardexRow>();
                return View();
            }

            var producto = await _context.Productos.FindAsync(productoId.Value);
            ViewBag.ProductoNombre = producto?.Nombre ?? "Producto";
            ViewBag.ProductoSKU = producto?.SKU ?? "";

            // Fetch movements for this product, sorted chronologically
            var movimientos = await _context.MovimientosAlmacen
                .Include(m => m.Usuario)
                .Where(m => m.ProductoId == productoId.Value)
                .OrderBy(m => m.Fecha)
                .ThenBy(m => m.Id)
                .ToListAsync();

            // Generate Kardex with Running Balance
            var kardex = new List<KardexRow>();
            int saldo = 0;

            foreach (var m in movimientos)
            {
                int entrada = m.Tipo == TipoMovimiento.Entrada ? m.Cantidad : 0;
                int salida = m.Tipo == TipoMovimiento.Salida ? m.Cantidad : 0;
                
                // Ajustes pueden ser positivos o negativos
                if (m.Tipo == TipoMovimiento.Ajuste)
                {
                    // For now, treat Ajuste as exit (loss)
                    salida = m.Cantidad;
                }

                saldo = saldo + entrada - salida;

                kardex.Add(new KardexRow
                {
                    Fecha = m.Fecha,
                    Concepto = m.Referencia ?? "Movimiento",
                    Entrada = entrada,
                    Salida = salida,
                    Saldo = saldo,
                    CostoUnitario = m.CostoUnitario,
                    ValorTotal = saldo * m.CostoUnitario
                });
            }

            ViewBag.Kardex = kardex;
            return View();
        }

        // ============================================================
        // 2. LOSS & DAMAGES REPORT (Reporte de Mermas)
        // ============================================================
        public async Task<IActionResult> Mermas()
        {
            // Filter movements that are losses/damages
            var mermas = await _context.MovimientosAlmacen
                .Include(m => m.Producto)
                .Include(m => m.Usuario)
                .Where(m => m.Tipo == TipoMovimiento.Ajuste || 
                           (m.Referencia != null && m.Referencia.Contains("PÉRDIDA")))
                .OrderByDescending(m => m.Fecha)
                .ToListAsync();

            // Calculate Total Loss
            decimal totalPerdida = mermas.Sum(m => m.Cantidad * m.CostoUnitario);
            ViewBag.TotalPerdida = totalPerdida;

            // Breakdown for Chart (Damaged vs Lost)
            int dañados = mermas.Count(m => m.Referencia != null && m.Referencia.Contains("DAÑO"));
            int perdidos = mermas.Count(m => m.Referencia != null && (m.Referencia.Contains("PÉRDIDA") || m.Referencia.Contains("PERDIDO")));
            
            ViewBag.ChartLabels = JsonSerializer.Serialize(new[] { "Dañados", "Perdidos/Robados" });
            ViewBag.ChartData = JsonSerializer.Serialize(new[] { dañados, perdidos });

            return View(mermas);
        }

        // ============================================================
        // 3. GRAPHS
        // ============================================================
        public async Task<IActionResult> Graficos()
        {
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

            var etiquetasMeses = datosMovimientos.Select(x => new DateTime(x.Anio, x.Mes, 1).ToString("MMM yyyy")).ToArray();
            var dataEntradas = datosMovimientos.Select(x => x.Entradas).ToArray();
            var dataSalidas = datosMovimientos.Select(x => x.Salidas).ToArray();

            ViewBag.ChartLabels = JsonSerializer.Serialize(etiquetasMeses);
            ViewBag.ChartDataEntradas = JsonSerializer.Serialize(dataEntradas);
            ViewBag.ChartDataSalidas = JsonSerializer.Serialize(dataSalidas);

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

            ViewBag.TopProductosLabels = JsonSerializer.Serialize(topProductos.Select(x => x.Producto));
            ViewBag.TopProductosData = JsonSerializer.Serialize(topProductos.Select(x => x.Cantidad));

            var lotes = await _context.LotesInventario
                .Select(l => new { l.StockActual, l.CostoUnitario })
                .ToListAsync();

            var valorTotal = lotes.Sum(l => l.StockActual * l.CostoUnitario);
            ViewBag.ValorInventario = valorTotal;

            return View();
        }
    }

    // ============================================================
    // KARDEX ROW MODEL (Internal)
    // ============================================================
    public class KardexRow
    {
        public DateTime Fecha { get; set; }
        public string Concepto { get; set; } = "";
        public int Entrada { get; set; }
        public int Salida { get; set; }
        public int Saldo { get; set; }
        public decimal CostoUnitario { get; set; }
        public decimal ValorTotal { get; set; }
    }
}
