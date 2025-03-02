using ChatAISystem;
using ChatAISystem.Models;
using ChatAISystem.Permissions;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;  // Para acceder a CookieSecurePolicy y SameSiteMode

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddSignalR(); // Agregar SignalR

builder.Services.AddDbContext<ChatAIDBContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DbLoginContext"));
});

// Configuraci�n de la sesi�n: 
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // Tiempo de expiraci�n
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    // Asegurarse de que la cookie solo se env�e en conexiones seguras (HTTPS)
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    // Configurar SameSite (puedes usar Strict, Lax o None seg�n tus necesidades)
    options.Cookie.SameSite = SameSiteMode.Strict;
});

// Configurar la pol�tica de cookies para que siempre usen HTTPS
builder.Services.Configure<CookiePolicyOptions>(options =>
{
    options.Secure = CookieSecurePolicy.Always;
});

builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add<ValidateSessionAttribute>();
});
builder.Services.AddHttpContextAccessor();

builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.Preserve;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // HSTS se recomienda en producci�n
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// Aseg�rate de usar la sesi�n y la pol�tica de cookies
app.UseSession();
app.UseCookiePolicy();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Login}/{action=Index}/{id?}");

app.MapHub<ChatHub>("/chatHub"); // Agregar el Hub

app.Run();
