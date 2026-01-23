using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Sistema_Almacen.Models;

namespace Sistema_Almacen.Controllers
{
    /// <summary>
    /// Controlador encargado de manejar la autenticación de usuarios
    /// </summary>
    public class AccesoController : Controller
    {
        // NOTA: Por ahora usamos una lista en memoria para pruebas
        // En producción, esto debería venir de una base de datos
        private List<Usuario> _usuariosPrueba = new List<Usuario>
        {
            new Usuario { Id = 1, NombreUsuario = "01231420", Password = "jazzmin", Rol = "Admin" },
            new Usuario { Id = 2, NombreUsuario = "01230708", Password = "cinthia", Rol = "Empleado" }
        };

        /// <summary>
        /// GET: /Acceso/Login
        /// Muestra la página de inicio de sesión
        /// </summary>
        [HttpGet]
        public IActionResult Login()
        {
            // Si el usuario ya está autenticado, redirigir al Dashboard
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Dashboard");
            }

            return View();
        }

        /// <summary>
        /// POST: /Acceso/Login
        /// Procesa el formulario de login y autentica al usuario
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Login(string nombreUsuario, string password)
        {
            try
            {
                // Validar que los campos no estén vacíos
                if (string.IsNullOrWhiteSpace(nombreUsuario) || string.IsNullOrWhiteSpace(password))
                {
                    ViewBag.Error = "Por favor ingrese usuario y contraseña";
                    return View();
                }

                // Buscar el usuario en la "base de datos" (lista en memoria por ahora)
                var usuario = _usuariosPrueba.FirstOrDefault(u => 
                    u.NombreUsuario == nombreUsuario && 
                    u.Password == password);

                // Si no se encuentra el usuario, mostrar error
                if (usuario == null)
                {
                    ViewBag.Error = "Usuario o contraseña incorrectos";
                    return View();
                }

                // ===== CREAR LA COOKIE DE AUTENTICACIÓN =====
                
                // 1. Crear los Claims (información del usuario que se guardará en la cookie)
                var claims = new List<Claim>
                {
                    // Claim con el ID del usuario
                    new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
                    
                    // Claim con el nombre de usuario
                    new Claim(ClaimTypes.Name, usuario.NombreUsuario),
                    
                    // Claim con el rol (Admin o Empleado)
                    // Esto permite usar [Authorize(Roles = "Admin")] en otros controladores
                    new Claim(ClaimTypes.Role, usuario.Rol)
                };

                // 2. Crear la identidad del usuario con los claims
                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                // 3. Crear el principal de autenticación
                var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

                // 4. Propiedades de la cookie (opcional)
                var authProperties = new AuthenticationProperties
                {
                    // La cookie expira en 8 horas de inactividad
                    ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8),
                    
                    // Renovar la cookie si el usuario sigue activo
                    IsPersistent = true,
                    
                    // Permitir refrescar la cookie antes de que expire
                    AllowRefresh = true
                };

                // 5. Iniciar sesión (crear la cookie)
                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    claimsPrincipal,
                    authProperties);

                // ===== REDIRIGIR AL DASHBOARD =====
                return RedirectToAction("Index", "Dashboard");
            }
            catch (Exception ex)
            {
                // Manejo de errores
                ViewBag.Error = "Ocurrió un error al iniciar sesión. Intente nuevamente.";
                // En producción, aquí deberías loggear el error
                return View();
            }
        }

        /// <summary>
        /// GET: /Acceso/Salir
        /// Cierra la sesión del usuario y elimina la cookie de autenticación
        /// </summary>
        public async Task<IActionResult> Salir()
        {
            // Eliminar la cookie de autenticación
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            // Redirigir a la página de login
            return RedirectToAction("Login", "Acceso");
        }
    }
}
