using Microsoft.EntityFrameworkCore;
using SWebEnergia.Models;
using Rotativa.AspNetCore;
using System.Globalization; // Asegúrate de tener este using
using Microsoft.AspNetCore.Localization; // Asegúrate de tener este using

var builder = WebApplication.CreateBuilder(args);

// Configurar EF Core con logging sensible habilitado (solo desarrollo)
builder.Services.AddDbContext<EnergiaContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("BDEnergia"))
           .EnableSensitiveDataLogging()
);

builder.Services.AddControllersWithViews();

builder.Services.AddHttpContextAccessor();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// 👇 Registrar tu servicio en segundo plano
builder.Services.AddHostedService<SWebEnergia.Services.AlertasBackgroundService>();
// 👇 Registrar el servicio de correo electrónico
builder.Services.AddTransient<SWebEnergia.Services.EmailService>();

var app = builder.Build();

// Crear la base de datos y tablas si no existen
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<EnergiaContext>();
    context.Database.EnsureCreated();
}

// ----------------------------------------------------------------------
// ✅ SOLUCIÓN PARA EL SÍMBOLO DE MONEDA (S/.) EN AMBIENTES DE HOSTING
// ----------------------------------------------------------------------

var defaultCulture = new CultureInfo("es-PE");
var localizationOptions = new RequestLocalizationOptions
{
    // Establece la cultura predeterminada para el formato de números, fechas y monedas
    DefaultRequestCulture = new RequestCulture(defaultCulture),
    // Soporta explícitamente solo la cultura peruana para esta aplicación
    SupportedCultures = new List<CultureInfo> { defaultCulture },
    SupportedUICultures = new List<CultureInfo> { defaultCulture }
};

// Middleware
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

// 👇 APLICA LA CONFIGURACIÓN DE CULTURA ANTES DE USE ROUTING
app.UseRequestLocalization(localizationOptions);

app.UseRouting();
app.UseSession();
app.UseAuthorization();

// 👉 Añadir esta línea para configurar Rotativa correctamente
RotativaConfiguration.Setup(app.Environment.WebRootPath, "Rotativa");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();