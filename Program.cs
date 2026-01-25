using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Sistema_Almacen.Data;
using Sistema_Almacen.Models;
using Sistema_Almacen.Services;
using BCrypt.Net;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Configurar DbContext con SQLite
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// Registrar servicios de negocio
builder.Services.AddScoped<IVentaService, VentaService>();

// ===== CONFIGURAR AUTENTICACIÓN CON COOKIES =====
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        // Ruta a la que se redirige si el usuario no está autenticado
        options.LoginPath = "/Acceso/Login";
        
        // Ruta a la que se redirige si el usuario no tiene permisos
        options.AccessDeniedPath = "/Acceso/Login";
        
        // Nombre de la cookie
        options.Cookie.Name = "SistemaAlmacenCookie";
        
        // Tiempo de expiración de la cookie (8 horas)
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        
        // La cookie expira si el usuario está inactivo
        options.SlidingExpiration = true;
    });

var app = builder.Build();

// ===== INICIALIZAR BASE DE DATOS =====
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        
        // ⚠️ SOLO PARA DESARROLLO: Descomentar la siguiente línea para resetear la BD
        // context.Database.EnsureDeleted();
        
        // Crear la base de datos si no existe (no borra datos existentes)
        context.Database.EnsureCreated(); 

        // 1. Seed Categorías (REQUERIDO por Producto)
        if (!context.Categorias.Any())
        {
            context.Categorias.Add(new Categoria { Nombre = "General" });
            context.SaveChanges();
        }

        // 2. Seed Ubicaciones (REQUERIDO por Lote)
        if (!context.Ubicaciones.Any())
        {
            context.Ubicaciones.Add(new Ubicacion { Nombre = "Almacén Central", Codigo = "ALM-01" });
            context.SaveChanges();
        }

        // 3. Seed Proveedores
        if (!context.Proveedores.Any())
        {
            context.Proveedores.Add(new Proveedor { Nombre = "Proveedor Genérico", Contacto = "Admin" });
            context.SaveChanges();
        }

        // 4. Seed Usuarios (contraseñas hasheadas con BCrypt)
        if (!context.Usuarios.Any())
        {
            context.Usuarios.AddRange(
                new Usuario 
                { 
                    NombreUsuario = "01231420", 
                    Password = BCrypt.Net.BCrypt.HashPassword("jazzmin"), 
                    Rol = "Admin" 
                },
                new Usuario 
                { 
                    NombreUsuario = "01230708", 
                    Password = BCrypt.Net.BCrypt.HashPassword("cinthia"), 
                    Rol = "Empleado" 
                }
            );
        }

        // 5. Seed Sucursales
        if (!context.Sucursales.Any())
        {
            context.Sucursales.AddRange(
                new Sucursal { Nombre = "Sucursal Norte" },
                new Sucursal { Nombre = "Sucursal Sur" }
            );
        }

        context.SaveChanges();
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Un error ocurrió al inicializar la base de datos.");
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// ===== IMPORTANTE: Agregar estos middlewares en este orden =====
app.UseAuthentication();  // Primero autenticación
app.UseAuthorization();   // Luego autorización

// Ruta por defecto apunta a la página de Login
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Acceso}/{action=Login}/{id?}");

app.Run();
