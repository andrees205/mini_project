using System.ComponentModel.DataAnnotations;
using TechSupport.API.Models;

namespace TechSupport.API.DTOs;

public class CrearIncidenteDto
{
    [Required(ErrorMessage = "El título es obligatorio.")]
    [StringLength(150, MinimumLength = 3, ErrorMessage = "El título debe tener entre 3 y 150 caracteres.")]
    public string Titulo { get; set; } = string.Empty;

    [Required(ErrorMessage = "La descripción es obligatoria.")]
    [StringLength(2000, ErrorMessage = "La descripción no puede superar 2000 caracteres.")]
    public string Descripcion { get; set; } = string.Empty;

    [Required(ErrorMessage = "La prioridad es obligatoria (Baja, Media, Alta o Critica).")]
    [EnumDataType(typeof(Prioridad), ErrorMessage = "Prioridad no válida (Baja, Media, Alta o Critica).")]
    public Prioridad? Prioridad { get; set; }
}

public record IncidenteDto(
    int Id,
    string Titulo,
    string Descripcion,
    Prioridad Prioridad,
    EstadoIncidente Estado,
    int UsuarioId,
    string? Usuario,
    int? TecnicoId,
    string? Tecnico,
    DateTime FechaCreacion,
    DateTime? FechaResolucion)
{
    public static IncidenteDto FromEntity(Incidente i) => new(
        i.Id, i.Titulo, i.Descripcion, i.Prioridad, i.Estado,
        i.UsuarioId, i.Usuario?.NombreUsuario,
        i.TecnicoId, i.Tecnico?.NombreUsuario,
        i.FechaCreacion, i.FechaResolucion);
}
