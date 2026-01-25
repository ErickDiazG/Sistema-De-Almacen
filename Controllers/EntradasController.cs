using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Sistema_Almacen.Data;
using Sistema_Almacen.Models;
using Sistema_Almacen.Models.DTOs;
using System.Security.Claims;

namespace Sistema_Almacen.Controllers
{
    [Authorize]
    public class EntradasController : Controller
    {
        private readonly ApplicationDbContext _context;

        public EntradasController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Create()
        {
            await CargarCombos();
            return View();
        }

        /// <summary>
        /// POST: Procesa la entrada de mercancía al almacén.
        /// Crea un nuevo lote de inventario y registra el movimiento de auditoría.
        /// </summary>
        /// <param name="dto">Datos de la entrada encapsulados en DTO</param>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(EntradaCreateDto dto)
        {
            if (!ModelState.IsValid)
            {
                await CargarCombos();
                return View();
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. Crear el Lote de Inventario (Motor FIFO)
                var lote = new LoteInventario
                {
                    ProductoId = dto.ProductoId,
                    ProveedorId = dto.ProveedorId,
                    UbicacionId = dto.UbicacionId,
                    SucursalId = dto.SucursalId,
                    CantidadInicial = dto.Cantidad,
                    StockActual = dto.Cantidad,
                    CostoUnitario = dto.CostoUnitario,
                    FechaEntrada = DateTime.Now
                };

                _context.LotesInventario.Add(lote);

                // 2. Registrar el Movimiento de Almacén (Auditoría)
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var movimiento = new MovimientoAlmacen
                {
                    Fecha = DateTime.Now,
                    UsuarioId = int.Parse(userId!),
                    Tipo = TipoMovimiento.Entrada,
                    Cantidad = dto.Cantidad,
                    Referencia = $"Entrada de mercancía - Lote #{lote.Id}"
                };

                _context.MovimientosAlmacen.Add(movimiento);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["Success"] = "Lote generado correctamente. Stock actualizado.";
                return RedirectToAction(nameof(Create));
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                ModelState.AddModelError("", "Ocurrió un error al procesar la entrada. Intente nuevamente.");
            }

            await CargarCombos();
            return View();
        }

        private async Task CargarCombos()
        {
            var productos = await _context.Productos
                .Select(p => new { p.Id, Display = $"{p.SKU} - {p.Nombre}" })
                .ToListAsync();

            ViewData["ProductoId"] = new SelectList(productos, "Id", "Display");
            ViewData["ProveedorId"] = new SelectList(await _context.Proveedores.ToListAsync(), "Id", "Nombre");
            ViewData["UbicacionId"] = new SelectList(await _context.Ubicaciones.ToListAsync(), "Id", "Nombre");
            ViewData["SucursalId"] = new SelectList(await _context.Sucursales.ToListAsync(), "Id", "Nombre");
        }
    }
}
