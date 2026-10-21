using System.ComponentModel.DataAnnotations;

namespace InventarioTic.Web.Models;

public enum EstadoActivo { EnUso, EnBodega, EnReparacion, Baja, Perdido }

public class Sede
{
    public int Id { get; set; }
    [Required, StringLength(100)] public string Nombre { get; set; } = "";
    [StringLength(200)] public string? Direccion { get; set; }
    public bool Activa { get; set; } = true;
    public List<Area> Areas { get; set; } = new();
}

public class Area
{
    public int Id { get; set; }
    public int SedeId { get; set; }
    public Sede? Sede { get; set; }
    [Required, StringLength(100)] public string Nombre { get; set; } = "";
}

public class Categoria
{
    public int Id { get; set; }
    [Required, StringLength(100)] public string Nombre { get; set; } = "";
    [Required, StringLength(6)] public string Prefijo { get; set; } = "";
}

public class Proveedor
{
    public int Id { get; set; }
    [Required, StringLength(150)] public string Nombre { get; set; } = "";
    [StringLength(30)] public string? Nit { get; set; }
    [StringLength(100)] public string? Contacto { get; set; }
    [StringLength(30)] public string? Telefono { get; set; }
    [StringLength(150)] public string? Correo { get; set; }
}

public class Empleado
{
    public int Id { get; set; }
    [Required, StringLength(100)] public string UsuarioDominio { get; set; } = "";
    [Required, StringLength(150)] public string Nombre { get; set; } = "";
    [StringLength(150)] public string? Correo { get; set; }
    [StringLength(100)] public string? Cargo { get; set; }
    public int? AreaId { get; set; }
    public Area? Area { get; set; }
    public bool Activo { get; set; } = true;
}

public class Activo
{
    public int Id { get; set; }
    [StringLength(20)] public string Codigo { get; set; } = "";

    [Display(Name = "Categoría")] public int CategoriaId { get; set; }
    public Categoria? Categoria { get; set; }

    [Required, StringLength(80)] public string Marca { get; set; } = "";
    [Required, StringLength(120)] public string Modelo { get; set; } = "";
    [StringLength(100)] public string? Serial { get; set; }
    public EstadoActivo Estado { get; set; } = EstadoActivo.EnBodega;

    [Display(Name = "Sede")] public int SedeId { get; set; }
    public Sede? Sede { get; set; }
    [Display(Name = "Área")] public int? AreaId { get; set; }
    public Area? Area { get; set; }

    [Display(Name = "Fecha de compra"), DataType(DataType.Date)]
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
    public DateTime? FechaCompra { get; set; }
    public decimal? Costo { get; set; }
    public int? ProveedorId { get; set; }
    public Proveedor? Proveedor { get; set; }
    [StringLength(50)] public string? Factura { get; set; }

    [Display(Name = "Garantía hasta"), DataType(DataType.Date)]
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
    public DateTime? GarantiaHasta { get; set; }
    [Display(Name = "Vida útil (años)")] public int? VidaUtilAnios { get; set; }

    [StringLength(60)] public string? Hostname { get; set; }
    [Display(Name = "Dirección IP"), StringLength(45)] public string? IP { get; set; }
    [StringLength(30)] public string? MAC { get; set; }
    [Display(Name = "Sistema operativo"), StringLength(80)] public string? SistemaOperativo { get; set; }
    [StringLength(100)] public string? Procesador { get; set; }
    [Display(Name = "RAM (GB)")] public int? RamGB { get; set; }
    [StringLength(100)] public string? Almacenamiento { get; set; }
    [StringLength(1000)] public string? Observaciones { get; set; }

    public DateTime CreadoEn { get; set; }
    public DateTime ActualizadoEn { get; set; }
    public List<Asignacion> Asignaciones { get; set; } = new();
}

public class Asignacion
{
    public int Id { get; set; }
    public int ActivoId { get; set; }
    public Activo? Activo { get; set; }
    public int EmpleadoId { get; set; }
    public Empleado? Empleado { get; set; }
    public DateTime FechaInicio { get; set; }
    public DateTime? FechaFin { get; set; }
    [StringLength(500)] public string? Observaciones { get; set; }
}
