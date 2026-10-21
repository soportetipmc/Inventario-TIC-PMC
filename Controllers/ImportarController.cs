using System.Text;
using InventarioTic.Web.Data;
using InventarioTic.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InventarioTic.Web.Controllers;

[Authorize(Policy = "Tecnico")]
public class ImportarController(AppDbContext db) : Controller
{
    const string Encabezado = "Categoria;Marca;Modelo;Serial;Sede;Area;Hostname;Responsable";

    public IActionResult Index() => View();

    public IActionResult Plantilla() =>
        File(new UTF8Encoding(true).GetBytes(Encabezado + "\r\nPortátil;Dell;Latitude 5420;ABC123;Sede 1;Sistemas;POR-SIS-01;jperez\r\n"),
            "text/csv", "plantilla_activos.csv");

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(IFormFile? archivo)
    {
        if (archivo is null || archivo.Length == 0)
        {
            ViewBag.Errores = new List<string> { "Selecciona un archivo CSV." };
            return View();
        }

        var errores = new List<string>();
        var nuevos = new List<Activo>();
        var cats = await db.Categorias.ToDictionaryAsync(c => c.Nombre.ToLower());
        var sedes = await db.Sedes.ToDictionaryAsync(s => s.Nombre.ToLower());
        var areas = await db.Areas.ToListAsync();
        var emps = await db.Empleados.ToDictionaryAsync(e => e.UsuarioDominio.ToLower());
        var seriales = (await db.Activos.Where(a => a.Serial != null).Select(a => a.Serial!).ToListAsync())
            .Select(s => s.ToLower()).ToHashSet();
        var codigos = await db.Activos.Select(a => a.Codigo).ToListAsync();
        var consecutivo = new Dictionary<string, int>();

        int Maximo(string p) => codigos.Where(x => x.StartsWith(p + "-"))
            .Select(x => int.TryParse(x[(p.Length + 1)..], out var v) ? v : 0).DefaultIfEmpty(0).Max();

        using var sr = new StreamReader(archivo.OpenReadStream(), Encoding.UTF8);
        await sr.ReadLineAsync(); // encabezado
        string? linea;
        int fila = 1;
        while ((linea = await sr.ReadLineAsync()) != null)
        {
            fila++;
            if (string.IsNullOrWhiteSpace(linea)) continue;
            var c = linea.Split(';').Select(x => x.Trim()).Concat(Enumerable.Repeat("", 8)).Take(8).ToArray();

            if (!cats.TryGetValue(c[0].ToLower(), out var cat)) { errores.Add($"Fila {fila}: la categoría «{c[0]}» no existe."); continue; }
            if (!sedes.TryGetValue(c[4].ToLower(), out var sede)) { errores.Add($"Fila {fila}: la sede «{c[4]}» no existe."); continue; }
            if (c[1] == "" || c[2] == "") { errores.Add($"Fila {fila}: marca y modelo son obligatorios."); continue; }

            Area? area = null;
            if (c[5] != "")
            {
                area = areas.FirstOrDefault(a => a.SedeId == sede.Id && a.Nombre.Equals(c[5], StringComparison.OrdinalIgnoreCase));
                if (area is null) { errores.Add($"Fila {fila}: el área «{c[5]}» no existe en {sede.Nombre}."); continue; }
            }
            Empleado? emp = null;
            if (c[7] != "" && !emps.TryGetValue(c[7].ToLower(), out emp))
            { errores.Add($"Fila {fila}: el usuario «{c[7]}» no está en Empleados."); continue; }
            if (c[3] != "" && !seriales.Add(c[3].ToLower())) { errores.Add($"Fila {fila}: el serial «{c[3]}» ya existe."); continue; }

            var n = (consecutivo.TryGetValue(cat.Prefijo, out var m) ? m : Maximo(cat.Prefijo)) + 1;
            consecutivo[cat.Prefijo] = n;
            var ahora = DateTime.Now;
            var activo = new Activo
            {
                Codigo = $"{cat.Prefijo}-{n:0000}", CategoriaId = cat.Id, Marca = c[1], Modelo = c[2],
                Serial = c[3] == "" ? null : c[3], SedeId = sede.Id, AreaId = area?.Id,
                Hostname = c[6] == "" ? null : c[6], Estado = emp is null ? EstadoActivo.EnBodega : EstadoActivo.EnUso,
                CreadoEn = ahora, ActualizadoEn = ahora
            };
            if (emp is not null) activo.Asignaciones.Add(new Asignacion { EmpleadoId = emp.Id, FechaInicio = ahora });
            nuevos.Add(activo);
        }

        if (errores.Count > 0) { ViewBag.Errores = errores; return View(); }
        db.Activos.AddRange(nuevos);
        await db.SaveChangesAsync();
        ViewBag.Creados = nuevos.Count;
        return View();
    }
}
