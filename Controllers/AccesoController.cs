using Microsoft.AspNetCore.Mvc;
using Sistema_Almacen.Models; 
using Sistema_Almacen.Data;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;

namespace Sistema_Almacen.Controllers
{
    public class AccesoController : Controller
    {
        public IActionResult Login()
        {
            ClaimsPrincipal claimUser = HttpContext.User;

            if (claimUser.Identity.IsAuthenticated)
                return RedirectToAction("Index", "Home");

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Ingresar(string rol)
        {
            // Validar que el rol sea uno de los permitidos
            if (rol != "Admin" && rol != "Empleado")
            {
                ViewData["Mensaje"] = "Rol no válido";
                return View("Login");
            }

            // Crear la identidad del usuario
            List<Claim> claims = new List<Claim>() {
                new Claim(ClaimTypes.Name, rol == "Admin" ? "Administrador" : "Empleado"),
                new Claim(ClaimTypes.Role, rol)
            };

            ClaimsIdentity claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            AuthenticationProperties properties = new AuthenticationProperties() { 
                AllowRefresh = true,
                IsPersistent = true // Mantener sesión iniciada
            };

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity), properties);

            return RedirectToAction("Index", "Home");
        }

        public async Task<IActionResult> Salir()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Acceso");
        }
    }
}
