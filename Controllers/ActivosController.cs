using InventarioTic.Web.Data;
using InventarioTic.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QRCoder;

namespace InventarioTic.Web.Controllers;

[Authorize(Policy = "Consulta")]
public class ActivosController(AppDbContext db, IConfiguration cfg) : Controller
{
    public async Task<IActionResult> Index(string? q, int? sedeId, int? categoriaId, EstadoActivo? estado)
    {
        var query = db.Activos
            .Include(a => a.Categoria).Include(a => a.Sede).Include(a => a.Area)
            .Include(a => a.Asignaciones.Where(x => x.FechaFin == null)).ThenInclude(x => x.Empleado)
            .AsQueryable();

        query = Filtrar(query, q, sedeId, categoriaId, estado);

        ViewBag.Q = q;
        ViewBag.Sedes = (await db.Sedes.OrderBy(s => s.Nombre).ToListAsync())
            .Select(s => new SelectListItem(s.Nombre, s.Id.ToString(), s.Id == sedeId)).ToList();
        ViewBag.Categorias = (await db.Categorias.OrderBy(c => c.Nombre).ToListAsync())
            .Select(c => new SelectListItem(c.Nombre, c.Id.ToString(), c.Id == categoriaId)).ToList();
        ViewBag.Estados = Enum.GetValues<EstadoActivo>()
            .Select(e => new SelectListItem(e.Texto(), e.ToString(), e == estado)).ToList();

        return View(await query.OrderBy(a => a.Codigo).ToListAsync());
    }

    public async Task<IActionResult> Details(int id)
    {
        var a = await db.Activos
            .Include(x => x.Categoria).Include(x => x.Sede).Include(x => x.Area).Include(x => x.Proveedor)
            .Include(x => x.Asignaciones).ThenInclude(s => s.Empleado)
            .FirstOrDefaultAsync(x => x.Id == id);
        return a is null ? NotFound() : View(a);
    }

    [Authorize(Policy = "Tecnico")]
    public async Task<IActionResult> Create()
    {
        await CargarListas();
        return View(new Activo());
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = "Tecnico")]
    public async Task<IActionResult> Create(Activo a)
    {
        ModelState.Remove(nameof(Activo.Codigo));
        await ValidarArea(a);
        if (!ModelState.IsValid) { await CargarListas(); return View(a); }

        a.Codigo = await SiguienteCodigo(a.CategoriaId);
        a.CreadoEn = a.ActualizadoEn = DateTime.Now;
        db.Activos.Add(a);
        await db.SaveChangesAsync();
        return RedirectToAction(nameof(Details), new { id = a.Id });
    }

    [Authorize(Policy = "Tecnico")]
    public async Task<IActionResult> Edit(int id)
    {
        var a = await db.Activos.FindAsync(id);
        if (a is null) return NotFound();
        await CargarListas();
        return View(a);
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = "Tecnico")]
    public async Task<IActionResult> Edit(int id, Activo form)
    {
        var original = await db.Activos.Include(x => x.Asignaciones.Where(s => s.FechaFin == null))
            .FirstOrDefaultAsync(x => x.Id == id);
        if (original is null) return NotFound();

        ModelState.Remove(nameof(Activo.Codigo));
        await ValidarArea(form);
        if (!ModelState.IsValid) { form.Codigo = original.Codigo; await CargarListas(); return View(form); }

        var codigo = original.Codigo;
        var creado = original.CreadoEn;
        form.Id = id;
        db.Entry(original).CurrentValues.SetValues(form);
        original.Codigo = codigo;
        original.CreadoEn = creado;
        original.ActualizadoEn = DateTime.Now;

        // Un activo dado de baja o perdido no puede seguir asignado.
        if (original.Estado is EstadoActivo.Baja or EstadoActivo.Perdido)
            foreach (var s in original.Asignaciones) s.FechaFin = DateTime.Now;

        await db.SaveChangesAsync();
        return RedirectToAction(nameof(Details), new { id });
    }

    [Authorize(Policy = "Tecnico")]
    public async Task<IActionResult> Asignar(int id)
    {
        var a = await db.Activos.FindAsync(id);
        if (a is null) return NotFound();
        ViewBag.Empleados = await ListaEmpleados();
        return View(a);
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = "Tecnico")]
    public async Task<IActionResult> Asignar(int id, int empleadoId, string? observaciones)
    {
        var a = await db.Activos.Include(x => x.Asignaciones.Where(s => s.FechaFin == null))
            .FirstOrDefaultAsync(x => x.Id == id);
        if (a is null) return NotFound();
        if (a.Estado is EstadoActivo.Baja or EstadoActivo.Perdido)
        {
            TempData["Mensaje"] = "Un activo dado de baja o perdido no se puede asignar.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var ahora = DateTime.Now;
        if (a.Asignaciones.Count > 0)
        {
            foreach (var s in a.Asignaciones) s.FechaFin = ahora;
            await db.SaveChangesAsync(); // cierra la anterior antes de abrir la nueva (índice único)
        }
        db.Asignaciones.Add(new Asignacion { ActivoId = id, EmpleadoId = empleadoId, FechaInicio = ahora, Observaciones = observaciones });
        a.Estado = EstadoActivo.EnUso;
        a.ActualizadoEn = ahora;
        await db.SaveChangesAsync();
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = "Tecnico")]
    public async Task<IActionResult> Devolver(int id)
    {
        var a = await db.Activos.Include(x => x.Asignaciones.Where(s => s.FechaFin == null))
            .FirstOrDefaultAsync(x => x.Id == id);
        if (a is null) return NotFound();
        foreach (var s in a.Asignaciones) s.FechaFin = DateTime.Now;
        a.Estado = EstadoActivo.EnBodega;
        a.ActualizadoEn = DateTime.Now;
        await db.SaveChangesAsync();
        return RedirectToAction(nameof(Details), new { id });
    }

    public async Task<IActionResult> Etiqueta(int id)
    {
        var a = await db.Activos.Include(x => x.Categoria).Include(x => x.Sede).FirstOrDefaultAsync(x => x.Id == id);
        return a is null ? NotFound() : View("Etiquetas", ConQr(new List<Activo> { a }));
    }

    public async Task<IActionResult> Etiquetas(string? q, int? sedeId, int? categoriaId, EstadoActivo? estado)
    {
        var query = Filtrar(db.Activos.Include(a => a.Categoria).Include(a => a.Sede).AsQueryable(), q, sedeId, categoriaId, estado);
        return View(ConQr(await query.OrderBy(a => a.Codigo).ToListAsync()));
    }

    private static IQueryable<Activo> Filtrar(IQueryable<Activo> query, string? q, int? sedeId, int? categoriaId, EstadoActivo? estado)
    {
        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(a => a.Codigo.Contains(q) || a.Marca.Contains(q) || a.Modelo.Contains(q)
                || (a.Serial != null && a.Serial.Contains(q)) || (a.Hostname != null && a.Hostname.Contains(q)));
        if (sedeId.HasValue) query = query.Where(a => a.SedeId == sedeId);
        if (categoriaId.HasValue) query = query.Where(a => a.CategoriaId == categoriaId);
        if (estado.HasValue) query = query.Where(a => a.Estado == estado);
        return query;
    }

    // El QR apunta a la ficha del activo (siempre actualizada y con permisos); en modo "Texto" lleva los datos directamente.
    private List<EtiquetaVm> ConQr(List<Activo> activos)
    {
        var baseUrl = cfg["Etiqueta:UrlBase"];
        if (string.IsNullOrWhiteSpace(baseUrl)) baseUrl = $"{Request.Scheme}://{Request.Host}";
        var modoTexto = string.Equals(cfg["Etiqueta:ContenidoQr"], "Texto", StringComparison.OrdinalIgnoreCase);
        using var gen = new QRCodeGenerator();
        return activos.Select(a =>
        {
            var contenido = modoTexto
                ? $"{a.Codigo} | {a.Marca} {a.Modelo} | S/N {a.Serial ?? "-"} | {a.Sede?.Nombre}"
                : $"{baseUrl.TrimEnd('/')}/Activos/Details/{a.Id}";
            using var datos = gen.CreateQrCode(contenido, QRCodeGenerator.ECCLevel.M);
            var png = new PngByteQRCode(datos).GetGraphic(8);
            return new EtiquetaVm(a, "data:image/png;base64," + Convert.ToBase64String(png));
        }).ToList();
    }

    private async Task ValidarArea(Activo a)
    {
        if (a.AreaId.HasValue && !await db.Areas.AnyAsync(x => x.Id == a.AreaId && x.SedeId == a.SedeId))
            ModelState.AddModelError(nameof(Activo.AreaId), "El área no pertenece a la sede elegida.");
    }

    private async Task<string> SiguienteCodigo(int categoriaId)
    {
        var prefijo = (await db.Categorias.FindAsync(categoriaId))!.Prefijo;
        var codigos = await db.Activos.Where(a => a.Codigo.StartsWith(prefijo + "-")).Select(a => a.Codigo).ToListAsync();
        var max = codigos.Select(c => int.TryParse(c[(prefijo.Length + 1)..], out var n) ? n : 0).DefaultIfEmpty(0).Max();
        return $"{prefijo}-{max + 1:0000}";
    }

    private async Task<List<SelectListItem>> ListaEmpleados() =>
        (await db.Empleados.Where(e => e.Activo).OrderBy(e => e.Nombre).ToListAsync())
            .Select(e => new SelectListItem($"{e.Nombre} ({e.UsuarioDominio})", e.Id.ToString())).ToList();

    private async Task CargarListas()
    {
        ViewBag.Categorias = (await db.Categorias.OrderBy(c => c.Nombre).ToListAsync())
            .Select(c => new SelectListItem(c.Nombre, c.Id.ToString())).ToList();
        ViewBag.Sedes = (await db.Sedes.Where(s => s.Activa).OrderBy(s => s.Nombre).ToListAsync())
            .Select(s => new SelectListItem(s.Nombre, s.Id.ToString())).ToList();
        ViewBag.Areas = (await db.Areas.Include(a => a.Sede).OrderBy(a => a.Sede!.Nombre).ThenBy(a => a.Nombre).ToListAsync())
            .Select(a => new SelectListItem($"{a.Sede!.Nombre} / {a.Nombre}", a.Id.ToString())).ToList();
        ViewBag.Proveedores = (await db.Proveedores.OrderBy(p => p.Nombre).ToListAsync())
            .Select(p => new SelectListItem(p.Nombre, p.Id.ToString())).ToList();
        ViewBag.Estados = Enum.GetValues<EstadoActivo>().Select(e => new SelectListItem(e.Texto(), e.ToString())).ToList();
    }
}
