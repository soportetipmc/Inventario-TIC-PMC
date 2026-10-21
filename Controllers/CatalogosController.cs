using InventarioTic.Web.Data;
using InventarioTic.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InventarioTic.Web.Controllers;

[Authorize(Policy = "Consulta")]
public class CatalogosController(AppDbContext db) : Controller
{
    public async Task<IActionResult> Index()
    {
        ViewBag.Sedes = await db.Sedes.Include(s => s.Areas).OrderBy(s => s.Nombre).ToListAsync();
        ViewBag.Categorias = await db.Categorias.OrderBy(c => c.Nombre).ToListAsync();
        ViewBag.Proveedores = await db.Proveedores.OrderBy(p => p.Nombre).ToListAsync();
        return View();
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = "Admin")]
    public async Task<IActionResult> AddSede(string nombre, string? direccion)
    {
        if (string.IsNullOrWhiteSpace(nombre)) return Volver("Escribe el nombre de la sede.");
        db.Sedes.Add(new Sede { Nombre = nombre.Trim(), Direccion = direccion?.Trim() });
        await db.SaveChangesAsync();
        return Volver("Sede agregada.");
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = "Admin")]
    public async Task<IActionResult> AddArea(int sedeId, string nombre)
    {
        if (string.IsNullOrWhiteSpace(nombre)) return Volver("Escribe el nombre del área.");
        db.Areas.Add(new Area { SedeId = sedeId, Nombre = nombre.Trim() });
        await db.SaveChangesAsync();
        return Volver("Área agregada.");
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = "Admin")]
    public async Task<IActionResult> AddCategoria(string nombre, string prefijo)
    {
        if (string.IsNullOrWhiteSpace(nombre) || string.IsNullOrWhiteSpace(prefijo))
            return Volver("Escribe el nombre y el prefijo de la categoría.");
        prefijo = prefijo.Trim().ToUpperInvariant();
        if (prefijo.Length > 6 || await db.Categorias.AnyAsync(c => c.Prefijo == prefijo))
            return Volver("El prefijo debe tener máximo 6 caracteres y no repetirse.");
        db.Categorias.Add(new Categoria { Nombre = nombre.Trim(), Prefijo = prefijo });
        await db.SaveChangesAsync();
        return Volver("Categoría agregada.");
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = "Admin")]
    public async Task<IActionResult> AddProveedor(string nombre, string? nit)
    {
        if (string.IsNullOrWhiteSpace(nombre)) return Volver("Escribe el nombre del proveedor.");
        db.Proveedores.Add(new Proveedor { Nombre = nombre.Trim(), Nit = nit?.Trim() });
        await db.SaveChangesAsync();
        return Volver("Proveedor agregado.");
    }

    private IActionResult Volver(string mensaje)
    {
        TempData["Mensaje"] = mensaje;
        return RedirectToAction(nameof(Index));
    }
}
