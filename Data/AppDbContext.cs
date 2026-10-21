using InventarioTic.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace InventarioTic.Web.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Sede> Sedes => Set<Sede>();
    public DbSet<Area> Areas => Set<Area>();
    public DbSet<Categoria> Categorias => Set<Categoria>();
    public DbSet<Proveedor> Proveedores => Set<Proveedor>();
    public DbSet<Empleado> Empleados => Set<Empleado>();
    public DbSet<Activo> Activos => Set<Activo>();
    public DbSet<Asignacion> Asignaciones => Set<Asignacion>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        // Evita rutas de cascada múltiples en SQL Server: nada se borra en cascada.
        foreach (var fk in b.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys()))
            fk.DeleteBehavior = DeleteBehavior.Restrict;

        b.Entity<Activo>(e =>
        {
            e.HasIndex(a => a.Codigo).IsUnique();
            e.HasIndex(a => a.Serial).IsUnique().HasFilter("[Serial] IS NOT NULL");
            e.Property(a => a.Estado).HasConversion<string>().HasMaxLength(20);
            e.Property(a => a.Costo).HasPrecision(18, 2);
        });
        b.Entity<Empleado>().HasIndex(e => e.UsuarioDominio).IsUnique();
        b.Entity<Categoria>().HasIndex(c => c.Prefijo).IsUnique();
        // Un activo solo puede tener una asignación vigente (FechaFin nula).
        b.Entity<Asignacion>().HasIndex(a => a.ActivoId).IsUnique().HasFilter("[FechaFin] IS NULL");
    }
}

public static class Seeder
{
    public static void Run(AppDbContext db)
    {
        if (!db.Sedes.Any())
            db.Sedes.AddRange(new Sede { Nombre = "Oficina Barranquilla" }, new Sede { Nombre = "Oficina Codazzi" });
        if (!db.Categorias.Any())
            db.Categorias.AddRange(
                new Categoria { Nombre = "Computador de escritorio", Prefijo = "PC" },
                new Categoria { Nombre = "Portátil", Prefijo = "POR" },
                new Categoria { Nombre = "Monitor", Prefijo = "MON" },
                new Categoria { Nombre = "Teclado", Prefijo = "TEC" },
                new Categoria { Nombre = "Mouse", Prefijo = "MOU" },
                new Categoria { Nombre = "Equipo de red", Prefijo = "RED" },
                new Categoria { Nombre = "Impresora", Prefijo = "IMP" },
                new Categoria { Nombre = "Celular", Prefijo = "CEL" });
        db.SaveChanges();
    }
}
