using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sistema_Almacen.Data;
using Sistema_Almacen.Models;

namespace Sistema_Almacen.Controllers
{

    public class UbicacionesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public UbicacionesController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            return View(await _context.Ubicaciones.ToListAsync());
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Ubicacion ubicacion)
        {
            if (ModelState.IsValid)
            {
                // Validar código único
                if (await _context.Ubicaciones.AnyAsync(u => u.Codigo == ubicacion.Codigo))
                {
                    ModelState.AddModelError("Codigo", "El código de ubicación ya existe.");
                    return View(ubicacion);
                }

                _context.Add(ubicacion);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(ubicacion);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var ubicacion = await _context.Ubicaciones.FindAsync(id);
            if (ubicacion == null) return NotFound();
            
            return View(ubicacion);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Ubicacion ubicacion)
        {
            if (id != ubicacion.Id) return NotFound();

            if (ModelState.IsValid)
            {
                // Validar código único (excluyendo la actual)
                if (await _context.Ubicaciones.AnyAsync(u => u.Codigo == ubicacion.Codigo && u.Id != id))
                {
                    ModelState.AddModelError("Codigo", "El código de ubicación ya está asignado a otra ubicación.");
                    return View(ubicacion);
                }

                try
                {
                    _context.Update(ubicacion);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Ubicaciones.Any(e => e.Id == ubicacion.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(ubicacion);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var ubicacion = await _context.Ubicaciones.FindAsync(id);
            if (ubicacion == null) return Json(new { success = false, message = "No se encontró la ubicación." });

            // Verificar si tiene lotes asociados
            bool tieneLotes = await _context.LotesInventario.AnyAsync(l => l.UbicacionId == id);
            if (tieneLotes)
            {
                return Json(new { success = false, message = "No se puede eliminar porque tiene inventario asociado." });
            }

            _context.Ubicaciones.Remove(ubicacion);
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Ubicación eliminada correctamente." });
        }
    }
}
