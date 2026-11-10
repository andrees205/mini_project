using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TechSupport.API.Data;
using TechSupport.API.DTOs;
using TechSupport.API.Models;

namespace TechSupport.API.Controllers;

[ApiController]
[Route("api/usuarios")]
[Authorize(Roles = "Administrador")]
[Produces("application/json")]
public class UsuariosController(AppDbContext db) : ControllerBase
{
    [HttpGet("tecnicos")]
    [ProducesResponseType<IEnumerable<UsuarioResumenDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<UsuarioResumenDto>>> Tecnicos() =>
        Ok(await db.Usuarios.AsNoTracking()
            .Where(u => u.Rol == Rol.Tecnico)
            .OrderBy(u => u.NombreUsuario)
            .Select(u => new UsuarioResumenDto(u.Id, u.NombreUsuario))
            .ToListAsync());
}
