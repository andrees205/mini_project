using System.ComponentModel.DataAnnotations;

namespace TechSupport.Web.Models;

public class ErrorViewModel
{
    public string? RequestId { get; set; }

    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
}

public class LoginViewModel
{
    [Required(ErrorMessage = "Ingresa tu usuario.")]
    [Display(Name = "Usuario")]
    public string NombreUsuario { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ingresa tu contraseña.")]
    [DataType(DataType.Password)]
    [Display(Name = "Contraseña")]
    public string Password { get; set; } = string.Empty;

    public string? ReturnUrl { get; set; }
}

public class CrearIncidenteViewModel
{
    [Display(Name = "Título")]
    public string Titulo { get; set; } = string.Empty;

    [Display(Name = "Descripción")]
    public string Descripcion { get; set; } = string.Empty;

    [Display(Name = "Prioridad")]
    public string Prioridad { get; set; } = "Media";
}

public record LoginRespuesta(string Token, DateTime Expira, int UsuarioId, string NombreUsuario, string Rol);

public record Tecnico(int Id, string NombreUsuario);

public record Incidente(
    int Id,
    string Titulo,
    string Descripcion,
    string Prioridad,
    string Estado,
    int UsuarioId,
    string? Usuario,
    int? TecnicoId,
    string? Tecnico,
    DateTime FechaCreacion,
    DateTime? FechaResolucion)
{
    public string? SiguienteEstado => Estado switch
    {
        "Asignado" => "EnProceso",
        "EnProceso" => "Resuelto",
        "Resuelto" => "Cerrado",
        _ => null
    };
}

public class DetalleIncidenteViewModel
{
    public required Incidente Incidente { get; init; }
    public IReadOnlyList<Tecnico> Tecnicos { get; init; } = [];
    public bool EsAdministrador { get; init; }
    public bool EsTecnico { get; init; }

    public bool PuedeAsignar => EsAdministrador && Incidente.Estado is "Pendiente" or "Asignado" or "EnProceso";
    public bool PuedeAvanzar => (EsAdministrador || EsTecnico) && Incidente.SiguienteEstado is not null;
    public bool PuedeEliminar => EsAdministrador && Incidente.Estado != "Cerrado";
}

public static class Formato
{
    public static string Estado(string estado) => estado switch
    {
        "EnProceso" => "En proceso",
        _ => estado
    };

    public static string Prioridad(string prioridad) => prioridad == "Critica" ? "Crítica" : prioridad;

    public static string ColorEstado(string estado) => estado switch
    {
        "Pendiente" => "text-bg-secondary",
        "Asignado" => "text-bg-info",
        "EnProceso" => "text-bg-primary",
        "Resuelto" => "text-bg-success",
        "Cerrado" => "text-bg-dark",
        _ => "text-bg-light"
    };

    public static string ColorPrioridad(string prioridad) => prioridad switch
    {
        "Baja" => "text-bg-light border",
        "Media" => "text-bg-warning",
        "Alta" => "text-bg-danger",
        "Critica" => "bg-danger-subtle text-danger-emphasis border border-danger",
        _ => "text-bg-light"
    };

    public static string Fecha(DateTime? fecha) =>
        fecha is null ? "—" : fecha.Value.ToLocalTime().ToString("dd/MM/yyyy HH:mm");
}
