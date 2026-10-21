using System.DirectoryServices.AccountManagement;
using InventarioTic.Web.Data;
using InventarioTic.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InventarioTic.Web.Controllers;

[Authorize(Policy = "Consulta")]
public class EmpleadosController(AppDbContext db) : Controller
{
    public async Task<IActionResult> Index() =>
        View(await db.Empleados.OrderBy(e => e.Nombre).ToListAsync());

    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = "Tecnico")]
    public async Task<IActionResult> ImportarAD()
    {
        try
        {
            int nuevos = 0, actualizados = 0;
            var existentes = await db.Empleados.ToDictionaryAsync(e => e.UsuarioDominio.ToLower());
            using var ctx = new PrincipalContext(ContextType.Domain);
            using var buscador = new PrincipalSearcher(new UserPrincipal(ctx) { Enabled = true });

            foreach (var u in buscador.FindAll().OfType<UserPrincipal>())
            {
                if (string.IsNullOrWhiteSpace(u.SamAccountName) || string.IsNullOrWhiteSpace(u.DisplayName)) continue;
                var clave = u.SamAccountName.ToLower();
                if (existentes.TryGetValue(clave, out var e))
                {
                    e.Nombre = u.DisplayName; e.Correo = u.EmailAddress; e.Activo = true; actualizados++;
                }
                else
                {
                    db.Empleados.Add(new Empleado { UsuarioDominio = u.SamAccountName, Nombre = u.DisplayName, Correo = u.EmailAddress });
                    nuevos++;
                }
            }
            await db.SaveChangesAsync();
            TempData["Mensaje"] = $"Importación lista: {nuevos} nuevos y {actualizados} actualizados.";
        }
        catch (Exception ex)
        {
            TempData["Mensaje"] = "No se pudo leer Active Directory: " + ex.Message;
        }
        return RedirectToAction(nameof(Index));
    }
}
