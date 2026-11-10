using System.ComponentModel.DataAnnotations;

namespace TechSupport.API.DTOs;

public class LoginDto
{
    [Required(ErrorMessage = "El nombre de usuario es obligatorio.")]
    public string NombreUsuario { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    public string Password { get; set; } = string.Empty;
}

public record LoginResponseDto(string Token, DateTime Expira, int UsuarioId, string NombreUsuario, string Rol);

public record UsuarioResumenDto(int Id, string NombreUsuario);
