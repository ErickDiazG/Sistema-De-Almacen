using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Sistema_Almacen.Data;
using Sistema_Almacen.Models;
using Sistema_Almacen.Models.ViewModels;
using Sistema_Almacen.Services;

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

        // ============================================================
        // TRAFFIC LIGHT MONITOR (Dashboard)
        // ============================================================
        public async Task<IActionResult> Index()
        {
            var allLoans = await _context.Prestamos
                .Include(p => p.Producto)
                .Include(p => p.UsuarioRegistro)
                .OrderByDescending(p => p.FechaSalida)
                .ToListAsync();

            var model = new LoanMonitorViewModel();

            foreach (var loan in allLoans)
            {
                if (loan.Estatus == EstatusPrestamo.Devuelto)
                {
                    model.HistoryLoans.Add(loan);
                }
                else if (loan.EstaAtrasado)
                {
                    model.OverdueLoans.Add(loan);
                }
                else
                {
                    model.ActiveLoans.Add(loan);
                }
            }

            ViewBag.HasRedAlerts = model.OverdueLoans.Any();
            return View(model);
        }

        // ============================================================
        // RETURN WIZARD (Partial Returns & Condition Check)
        // ============================================================
        [HttpPost]
        public async Task<IActionResult> RegistrarDevolucion(int loanId, int cantidadDevolver, string condicion, string observaciones)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var prestamo = await _context.Prestamos
                    .Include(p => p.Producto)
                    .FirstOrDefaultAsync(p => p.Id == loanId);
                    
                if (prestamo == null) 
                    return Json(new { success = false, message = "Préstamo no encontrado" });

                if (prestamo.Estatus == EstatusPrestamo.Devuelto)
                    return Json(new { success = false, message = "Préstamo ya cerrado" });

                int pendientes = prestamo.Cantidad - prestamo.CantidadDevuelta;
                if (cantidadDevolver > pendientes)
                    return Json(new { success = false, message = $"Solo restan {pendientes} por devolver." });

                // Update Loan quantity
                prestamo.CantidadDevuelta += cantidadDevolver;
                decimal costoProducto = prestamo.Producto?.CostoPromedio ?? 0;

                if (condicion == "DAÑADO" || condicion == "PERDIDO")
                {
                    // Log Loss - DO NOT RETURN TO STOCK
                    _context.MovimientosAlmacen.Add(new MovimientoAlmacen
                    {
                        Fecha = DateTime.Now,
                        UsuarioId = 1,
                        Tipo = TipoMovimiento.Ajuste,
                        Cantidad = cantidadDevolver,
                        Referencia = $"PÉRDIDA/DAÑO Loan #{prestamo.Id}. Condición: {condicion}. Obs: {observaciones}",
                        ProductoId = prestamo.ProductoId,
                        CostoUnitario = costoProducto
                    });
                }
                else
                {
                    // Good Condition: Return to Stock
                    var ubicacion = await _context.Ubicaciones.FirstOrDefaultAsync();
                    var sucursal = await _context.Sucursales.FirstOrDefaultAsync();

                    var loteRetorno = new LoteInventario
                    {
                        ProductoId = prestamo.ProductoId,
                        CantidadInicial = cantidadDevolver,
                        StockActual = cantidadDevolver,
                        CostoUnitario = costoProducto,
                        FechaEntrada = DateTime.Now,
                        UbicacionId = ubicacion?.Id ?? 1,
                        SucursalId = sucursal?.Id ?? 1
                    };
                    _context.LotesInventario.Add(loteRetorno);

                    _context.MovimientosAlmacen.Add(new MovimientoAlmacen
                    {
                        Fecha = DateTime.Now,
                        UsuarioId = 1,
                        Tipo = TipoMovimiento.Entrada,
                        Cantidad = cantidadDevolver,
                        Referencia = $"Devolución Loan #{prestamo.Id}. Estado: Bueno.",
                        ProductoId = prestamo.ProductoId,
                        CostoUnitario = costoProducto
                    });
                }

                // Close Loan if fully returned
                if (prestamo.CantidadDevuelta >= prestamo.Cantidad)
                {
                    prestamo.Estatus = EstatusPrestamo.Devuelto;
                    prestamo.FechaDevolucionReal = DateTime.Now;
                    prestamo.Comentarios += $" | [CERRADO] {observaciones}";
                }
                else
                {
                    prestamo.Comentarios += $" | [PARCIAL -{cantidadDevolver}] {observaciones}";
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Json(new { success = true, message = "Devolución procesada correctamente." });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        // ============================================================
        // CRUD (Simplified)
        // ============================================================
        public async Task<IActionResult> Create()
        {
            await CargarCombos();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Prestamo prestamo)
        {
            return RedirectToAction(nameof(Index));
        }

        private async Task CargarCombos()
        {
            ViewData["ProductoId"] = new SelectList(await _context.Productos.ToListAsync(), "Id", "Nombre");
        }
    }
}
