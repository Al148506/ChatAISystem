using ChatAISystem;
using ChatAISystem.Models;
using ChatAISystem.Permissions;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using ChatAISystem.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// Inyección de servicios
builder.Services.AddControllersWithViews();
builder.Services.AddSignalR().AddAzureSignalR(options =>
{
    options.ConnectionString = builder.Configuration["Azure:SignalR:ConnectionString"];
});
builder.Services.AddDbContext<ChatAIDBContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DbLoginContext"));
});

// ✅ Configuración de la sesión
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
});

// ✅ Política de cookies
builder.Services.Configure<CookiePolicyOptions>(options =>
{
    options.Secure = CookieSecurePolicy.Always;
});

builder.Services.AddHttpContextAccessor();

builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.Preserve;
});

// ✅ Inyectar el filtro para validar la sesión
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add<ValidateSessionAttribute>();
});

// ✅ Inyectar el servicio para validar el usuario
builder.Services.AddScoped<IUserValidationService, UserValidationService>();

// Construir la app
var app = builder.Build();

// Middleware para el manejo de errores
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

// ✅ Habilitar la política de cookies
app.UseCookiePolicy();

// ✅ Habilitar las sesiones antes de la autorización
app.UseSession();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

// ✅ Endpoint para SignalR
app.UseEndpoints(endpoints =>
{
    endpoints.MapHub<ChatHub>("/chatHub");
});

// ✅ Rutas del controlador
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Login}/{action=Index}/{id?}");

app.Run();
