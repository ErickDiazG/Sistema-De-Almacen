using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sistema_Almacen.Data;
using Sistema_Almacen.Models;

namespace Sistema_Almacen.Controllers
{
    [Authorize]
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
    }
}
