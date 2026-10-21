using InventarioTic.Web.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Server.IIS;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Server.IISIntegration;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(o =>
    // SQL Server 2017 = nivel de compatibilidad 140.
    o.UseSqlServer(builder.Configuration.GetConnectionString("Inventario"), sql => sql.UseCompatibilityLevel(140)));

// Windows Authentication la resuelve IIS; los roles son los grupos de Active Directory.
builder.Services.AddAuthentication(IISDefaults.AuthenticationScheme);

var g = builder.Configuration.GetSection("Grupos");
string admin = g["Admin"]!, tecnico = g["Tecnico"]!, consulta = g["Consulta"]!;

builder.Services.AddAuthorization(o =>
{
    o.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
    o.AddPolicy("Admin", p => p.RequireAssertion(c => c.User.IsInRole(admin)));
    o.AddPolicy("Tecnico", p => p.RequireAssertion(c => c.User.IsInRole(admin) || c.User.IsInRole(tecnico)));
    o.AddPolicy("Consulta", p => p.RequireAssertion(c =>
        c.User.IsInRole(admin) || c.User.IsInRole(tecnico) || c.User.IsInRole(consulta)));
});

builder.Services.AddControllersWithViews();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
    Seeder.Run(db);
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllerRoute("default", "{controller=Activos}/{action=Index}/{id?}");
app.Run();
