using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Sistema_Almacen.Data;
using Sistema_Almacen.Models;
using Microsoft.AspNetCore.Hosting;
using System.IO;

namespace Sistema_Almacen.Controllers
{

    public class ProductosController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _hostEnvironment;

        public ProductosController(ApplicationDbContext context, IWebHostEnvironment hostEnvironment)
        {
            _context = context;
            _hostEnvironment = hostEnvironment;
        }

        public async Task<IActionResult> Index()
        {
            var productos = await _context.Productos.Include(p => p.Categoria).ToListAsync();
            return View(productos);
        }

        public async Task<IActionResult> Create()
        {
            ViewData["CategoriaId"] = new SelectList(await _context.Categorias.ToListAsync(), "Id", "Nombre");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Producto producto, IFormFile? imagenArchivo)
        {
            // Remover la propiedad de navegación del ModelState ya que solo necesitamos CategoriaId
            ModelState.Remove("Categoria");

            if (ModelState.IsValid)
            {
                // Validar SKU único
                if (await _context.Productos.AnyAsync(p => p.SKU == producto.SKU))
                {
                    ModelState.AddModelError("SKU", "El SKU ya existe.");
                    ViewData["CategoriaId"] = new SelectList(await _context.Categorias.ToListAsync(), "Id", "Nombre", producto.CategoriaId);
                    return View(producto);
                }

                // La imagen es opcional, solo guardar si se subió
                if (imagenArchivo != null && imagenArchivo.Length > 0)
                {
                    producto.ImagenURL = await GuardarImagen(imagenArchivo);
                }

                try
                {
                    _context.Add(producto);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Producto creado exitosamente.";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Error al guardar el producto: " + ex.Message);
                }
            }

            // Para debugging: mostrar errores de validación en consola
            foreach (var modelStateKey in ModelState.Keys)
            {
                var value = ModelState[modelStateKey];
                if (value != null && value.Errors.Count > 0)
                {
                    foreach (var error in value.Errors)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error en '{modelStateKey}': {error.ErrorMessage}");
                    }
                }
            }

            ViewData["CategoriaId"] = new SelectList(await _context.Categorias.ToListAsync(), "Id", "Nombre", producto.CategoriaId);
            return View(producto);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var producto = await _context.Productos.FindAsync(id);
            if (producto == null) return NotFound();
            
            ViewData["CategoriaId"] = new SelectList(await _context.Categorias.ToListAsync(), "Id", "Nombre", producto.CategoriaId);
            return View(producto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Producto producto, IFormFile? imagenArchivo)
        {
            if (id != producto.Id) return NotFound();

            if (ModelState.IsValid)
            {
                // Validar SKU único
                if (await _context.Productos.AnyAsync(p => p.SKU == producto.SKU && p.Id != id))
                {
                    ModelState.AddModelError("SKU", "El SKU ya está asignado a otro producto.");
                    ViewData["CategoriaId"] = new SelectList(await _context.Categorias.ToListAsync(), "Id", "Nombre", producto.CategoriaId);
                    return View(producto);
                }

                try
                {
                    if (imagenArchivo != null)
                    {
                        // Opcional: Borrar imagen anterior si existía
                        BorrarImagen(producto.ImagenURL);
                        producto.ImagenURL = await GuardarImagen(imagenArchivo);
                    }
                    else
                    {
                        // Mantener la imagen actual si no se subió una nueva
                        _context.Entry(producto).Property(x => x.ImagenURL).IsModified = false;
                    }

                    _context.Update(producto);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Productos.Any(e => e.Id == producto.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["CategoriaId"] = new SelectList(await _context.Categorias.ToListAsync(), "Id", "Nombre", producto.CategoriaId);
            return View(producto);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var producto = await _context.Productos.FindAsync(id);
            if (producto == null) return Json(new { success = false, message = "No se encontró el producto." });

            // Verificar si tiene lotes asociados
            bool tieneLotes = await _context.LotesInventario.AnyAsync(l => l.ProductoId == id);
            if (tieneLotes)
            {
                return Json(new { success = false, message = "No se puede eliminar porque tiene existencias en inventario." });
            }

            try
            {
                BorrarImagen(producto.ImagenURL);
                _context.Productos.Remove(producto);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Producto eliminado correctamente." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error al eliminar: " + ex.Message });
            }
        }

        private async Task<string> GuardarImagen(IFormFile archivo)
        {
            string wwwRootPath = _hostEnvironment.WebRootPath;
            string fileName = Guid.NewGuid().ToString() + Path.GetExtension(archivo.FileName);
            string productPath = Path.Combine(wwwRootPath, @"img\productos");

            if (!Directory.Exists(productPath))
            {
                Directory.CreateDirectory(productPath);
            }

            using (var fileStream = new FileStream(Path.Combine(productPath, fileName), FileMode.Create))
            {
                await archivo.CopyToAsync(fileStream);
            }

            return "/img/productos/" + fileName;
        }

        private void BorrarImagen(string? url)
        {
            if (string.IsNullOrEmpty(url)) return;

            string wwwRootPath = _hostEnvironment.WebRootPath;
            var oldImagePath = Path.Combine(wwwRootPath, url.TrimStart('\\'));
            if (System.IO.File.Exists(oldImagePath))
            {
                System.IO.File.Delete(oldImagePath);
            }
        }
    }
}
