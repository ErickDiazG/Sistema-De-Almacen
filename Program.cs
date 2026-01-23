using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

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
