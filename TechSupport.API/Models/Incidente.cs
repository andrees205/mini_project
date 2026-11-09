namespace TechSupport.API.Models;

public class Incidente
{
    public int Id { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public Prioridad Prioridad { get; set; }
    public EstadoIncidente Estado { get; set; } = EstadoIncidente.Pendiente;

    public int UsuarioId { get; set; }
    public Usuario? Usuario { get; set; }

    public int? TecnicoId { get; set; }
    public Usuario? Tecnico { get; set; }

    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

    public DateTime? FechaResolucion { get; set; }
}
