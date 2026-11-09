using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TechSupport.API.Data;
using TechSupport.API.DTOs;
using TechSupport.API.Models;

namespace TechSupport.API.Controllers;

[ApiController]
[Route("api/incidentes")]
public class IncidentesController(AppDbContext db) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<IncidenteDto>> Crear(CrearIncidenteDto dto)
    {
        if (!await db.Usuarios.AnyAsync(u => u.Id == dto.UsuarioId))
            return BadRequest(new { success = false, message = "El usuario indicado no existe." });

        var incidente = new Incidente
        {
            Titulo = dto.Titulo.Trim(),
            Descripcion = dto.Descripcion.Trim(),
            Prioridad = dto.Prioridad!.Value,
            Estado = EstadoIncidente.Pendiente,
            UsuarioId = dto.UsuarioId,
            FechaCreacion = DateTime.UtcNow
        };

        db.Incidentes.Add(incidente);
        await db.SaveChangesAsync();
        await db.Entry(incidente).Reference(i => i.Usuario).LoadAsync();

        return CreatedAtAction(nameof(ObtenerPorId), new { id = incidente.Id }, IncidenteDto.FromEntity(incidente));
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<IncidenteDto>>> Listar()
    {
        var incidentes = await db.Incidentes
            .Include(i => i.Usuario)
            .Include(i => i.Tecnico)
            .OrderByDescending(i => i.FechaCreacion)
            .AsNoTracking()
            .ToListAsync();

        return Ok(incidentes.Select(IncidenteDto.FromEntity));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<IncidenteDto>> ObtenerPorId(int id)
    {
        var incidente = await db.Incidentes
            .Include(i => i.Usuario)
            .Include(i => i.Tecnico)
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == id);

        return incidente is null ? NotFound() : Ok(IncidenteDto.FromEntity(incidente));
    }
}
