using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TechSupport.API.Data;
using TechSupport.API.DTOs;
using TechSupport.API.Services;

namespace TechSupport.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(AppDbContext db, ITokenService tokenService) : ControllerBase
{
    private const string MensajeCredencialesInvalidas = "Usuario o contraseña incorrectos.";

    private static readonly string HashFicticio = BCrypt.Net.BCrypt.HashPassword("hash-ficticio-para-igualar-tiempos");

    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType<LoginResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LoginResponseDto>> Login(LoginDto dto)
    {
        var usuario = await db.Usuarios.AsNoTracking()
            .FirstOrDefaultAsync(u => u.NombreUsuario == dto.NombreUsuario);

        var hash = usuario?.PasswordHash ?? HashFicticio;
        var passwordOk = BCrypt.Net.BCrypt.Verify(dto.Password, hash);

        if (usuario is null || !passwordOk)
            return Unauthorized(new { success = false, message = MensajeCredencialesInvalidas });

        var (token, expira) = tokenService.GenerarToken(usuario);
        return Ok(new LoginResponseDto(token, expira, usuario.Id, usuario.NombreUsuario, usuario.Rol.ToString()));
    }
}
