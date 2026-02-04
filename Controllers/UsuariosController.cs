using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sistema_Almacen.Data;
using Sistema_Almacen.Models;
using BCrypt.Net;

namespace Sistema_Almacen.Controllers
{

    public class UsuariosController : Controller
    {
        private readonly ApplicationDbContext _context;

        public UsuariosController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var usuarios = await _context.Usuarios.ToListAsync();
            return View(usuarios);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Usuario usuario)
        {
            if (ModelState.IsValid)
            {
                // Verificar si ya existe el usuario
                if (await _context.Usuarios.AnyAsync(u => u.NombreUsuario == usuario.NombreUsuario))
                {
                    ModelState.AddModelError("NombreUsuario", "Este número de empleado ya está registrado.");
                    return View(usuario);
                }

                // Hashear password
                usuario.Password = BCrypt.Net.BCrypt.HashPassword(usuario.Password);

                _context.Add(usuario);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(usuario);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var usuario = await _context.Usuarios.FindAsync(id);
            if (usuario == null) return NotFound();

            // No enviamos el password a la vista de edición
            usuario.Password = "";
            return View(usuario);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Nombre,NombreUsuario,Rol")] Usuario usuario, string? newPassword)
        {
            if (id != usuario.Id) return NotFound();

            // Removemos Password del ModelState porque no es obligatorio en edición si no se cambia
            ModelState.Remove("Password");

            if (ModelState.IsValid)
            {
                try
                {
                    var existingUser = await _context.Usuarios.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id);
                    if (existingUser == null) return NotFound();

                    // Verificar duplicados de NombreUsuario
                    if (await _context.Usuarios.AnyAsync(u => u.NombreUsuario == usuario.NombreUsuario && u.Id != id))
                    {
                        ModelState.AddModelError("NombreUsuario", "Este número de empleado ya está registrado por otro usuario.");
                        return View(usuario);
                    }

                    // Si se proporcionó un nuevo password, lo hasheamos. Si no, mantenemos el anterior.
                    if (!string.IsNullOrEmpty(newPassword))
                    {
                        usuario.Password = BCrypt.Net.BCrypt.HashPassword(newPassword);
                    }
                    else
                    {
                        usuario.Password = existingUser.Password;
                    }

                    _context.Update(usuario);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!UsuarioExists(usuario.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(usuario);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var usuario = await _context.Usuarios.FindAsync(id);
            if (usuario != null)
            {
                _context.Usuarios.Remove(usuario);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Usuario eliminado correctamente" });
            }
            return Json(new { success = false, message = "Error al eliminar usuario" });
        }

        private bool UsuarioExists(int id)
        {
            return _context.Usuarios.Any(e => e.Id == id);
        }
    }
}
