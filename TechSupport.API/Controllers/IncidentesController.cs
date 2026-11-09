using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechSupport.API.DTOs;
using TechSupport.API.Extensions;
using TechSupport.API.Services;

namespace TechSupport.API.Controllers;

[ApiController]
[Authorize]
[Route("api/incidentes")]
[Produces("application/json")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
public class IncidentesController(IIncidenteService service) : ControllerBase
{
    private Actor Actor => new(User.GetUsuarioId(), User.GetRol());

    [HttpPost]
    [ProducesResponseType<IncidenteDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IncidenteDto>> Crear(CrearIncidenteDto dto)
    {
        var creado = await service.CrearAsync(Actor, dto);
        return CreatedAtAction(nameof(ObtenerPorId), new { id = creado.Id }, creado);
    }

    [HttpGet]
    [ProducesResponseType<IEnumerable<IncidenteDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<IncidenteDto>>> Listar() =>
        Ok(await service.ListarAsync(Actor));

    [HttpGet("{id:int}")]
    [ProducesResponseType<IncidenteDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IncidenteDto>> ObtenerPorId(int id) =>
        ToActionResult(await service.ObtenerAsync(Actor, id));

    [HttpPut("{id:int}/asignar")]
    [Authorize(Roles = "Administrador")]
    [ProducesResponseType<IncidenteDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IncidenteDto>> Asignar(int id, AsignarTecnicoDto dto) =>
        ToActionResult(await service.AsignarTecnicoAsync(Actor, id, dto.TecnicoId!.Value));

    [HttpPut("{id:int}/estado")]
    [Authorize(Roles = "Administrador,Tecnico")]
    [ProducesResponseType<IncidenteDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IncidenteDto>> CambiarEstado(int id, CambiarEstadoDto dto) =>
        ToActionResult(await service.CambiarEstadoAsync(Actor, id, dto.NuevoEstado!.Value));

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Administrador")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Eliminar(int id)
    {
        var resultado = await service.EliminarAsync(Actor, id);
        return resultado.Tipo == TipoResultado.Ok ? NoContent() : Error(resultado.Tipo, resultado.Mensaje);
    }

    private ActionResult ToActionResult<T>(ResultadoServicio<T> r) =>
        r.Tipo == TipoResultado.Ok ? Ok(r.Valor) : Error(r.Tipo, r.Mensaje);

    private ObjectResult Error(TipoResultado tipo, string? mensaje)
    {
        var status = tipo switch
        {
            TipoResultado.NoEncontrado => StatusCodes.Status404NotFound,
            TipoResultado.Prohibido => StatusCodes.Status403Forbidden,
            _ => StatusCodes.Status400BadRequest
        };
        return StatusCode(status, new { success = false, message = mensaje });
    }
}
