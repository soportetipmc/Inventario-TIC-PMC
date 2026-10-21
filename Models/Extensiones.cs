namespace InventarioTic.Web.Models;

public static class Extensiones
{
    public static string Texto(this EstadoActivo e) => e switch
    {
        EstadoActivo.EnUso => "En uso",
        EstadoActivo.EnBodega => "En bodega",
        EstadoActivo.EnReparacion => "En reparación",
        EstadoActivo.Baja => "Baja",
        EstadoActivo.Perdido => "Perdido",
        _ => e.ToString()
    };
}

public record EtiquetaVm(Activo Activo, string Qr);
