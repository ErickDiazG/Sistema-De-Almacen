using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Sistema_Almacen.Controllers
{
    /// <summary>
    /// Controlador del Dashboard principal
    /// Solo accesible para usuarios autenticados
    /// </summary>
    [Authorize] // Requiere autenticación para acceder
    public class DashboardController : Controller
    {
        /// <summary>
        /// Página principal del dashboard
        /// Muestra información del usuario autenticado
        /// </summary>
        public IActionResult Index()
        {
            // Obtener el nombre del usuario autenticado
            var nombreUsuario = User.Identity.Name;
            
            // Obtener el rol del usuario
            var rol = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Role)?.Value;
            
            // Pasar información a la vista mediante ViewBag
            ViewBag.NombreUsuario = nombreUsuario;
            ViewBag.Rol = rol;
            
            return View();
        }
    }
}
