using Microsoft.EntityFrameworkCore;
using TechSupport.API.Data;
using TechSupport.API.DTOs;
using TechSupport.API.Models;

namespace TechSupport.API.Services;

public interface IIncidenteService
{
    Task<IncidenteDto> CrearAsync(Actor actor, CrearIncidenteDto dto);
    Task<List<IncidenteDto>> ListarAsync(Actor actor);
    Task<ResultadoServicio<IncidenteDto>> ObtenerAsync(Actor actor, int id);
    Task<ResultadoServicio<IncidenteDto>> AsignarTecnicoAsync(Actor actor, int id, int tecnicoId);
    Task<ResultadoServicio<IncidenteDto>> CambiarEstadoAsync(Actor actor, int id, EstadoIncidente nuevoEstado);
    Task<ResultadoServicio<bool>> EliminarAsync(Actor actor, int id);
}

public class IncidenteService(AppDbContext db, ILogger<IncidenteService> logger) : IIncidenteService
{
    public async Task<IncidenteDto> CrearAsync(Actor actor, CrearIncidenteDto dto)
    {
        var incidente = new Incidente
        {
            Titulo = dto.Titulo.Trim(),
            Descripcion = dto.Descripcion.Trim(),
            Prioridad = dto.Prioridad!.Value,
            Estado = EstadoIncidente.Pendiente,
            UsuarioId = actor.Id,
            FechaCreacion = DateTime.UtcNow
        };

        db.Incidentes.Add(incidente);
        await db.SaveChangesAsync();
        await db.Entry(incidente).Reference(i => i.Usuario).LoadAsync();

        logger.LogInformation("Incidente {IncidenteId} creado por usuario {UsuarioId}", incidente.Id, actor.Id);
        return IncidenteDto.FromEntity(incidente);
    }

    public async Task<List<IncidenteDto>> ListarAsync(Actor actor)
    {
        var incidentes = await Visibles(actor)
            .OrderByDescending(i => i.FechaCreacion)
            .AsNoTracking()
            .ToListAsync();

        return incidentes.Select(IncidenteDto.FromEntity).ToList();
    }

    public async Task<ResultadoServicio<IncidenteDto>> ObtenerAsync(Actor actor, int id)
    {
        var incidente = await Visibles(actor).AsNoTracking().FirstOrDefaultAsync(i => i.Id == id);

        return incidente is null
            ? ResultadoServicio<IncidenteDto>.NoEncontrado()
            : ResultadoServicio<IncidenteDto>.Ok(IncidenteDto.FromEntity(incidente));
    }

    public async Task<ResultadoServicio<IncidenteDto>> AsignarTecnicoAsync(Actor actor, int id, int tecnicoId)
    {
        if (actor.Rol != Rol.Administrador)
            return ResultadoServicio<IncidenteDto>.Prohibido();

        var incidente = await Visibles(actor).FirstOrDefaultAsync(i => i.Id == id);
        if (incidente is null)
            return ResultadoServicio<IncidenteDto>.NoEncontrado();

        if (incidente.Estado is EstadoIncidente.Resuelto or EstadoIncidente.Cerrado)
            return ResultadoServicio<IncidenteDto>.Invalido(
                $"No se puede asignar un técnico a un incidente en estado {incidente.Estado}.");

        var tecnico = await db.Usuarios.FirstOrDefaultAsync(u => u.Id == tecnicoId);
        if (tecnico is null || tecnico.Rol != Rol.Tecnico)
            return ResultadoServicio<IncidenteDto>.Invalido(
                $"El usuario {tecnicoId} no existe o no tiene rol Tecnico. Solo se puede asignar a técnicos.");

        incidente.TecnicoId = tecnico.Id;
        incidente.Tecnico = tecnico;

        if (incidente.Estado == EstadoIncidente.Pendiente)
            incidente.Estado = EstadoIncidente.Asignado;

        await db.SaveChangesAsync();
        logger.LogInformation("Incidente {IncidenteId} asignado al técnico {TecnicoId} por {ActorId}", id, tecnico.Id, actor.Id);
        return ResultadoServicio<IncidenteDto>.Ok(IncidenteDto.FromEntity(incidente));
    }

    public async Task<ResultadoServicio<IncidenteDto>> CambiarEstadoAsync(Actor actor, int id, EstadoIncidente nuevoEstado)
    {
        if (actor.Rol == Rol.Usuario)
            return ResultadoServicio<IncidenteDto>.Prohibido();

        var incidente = await Visibles(actor).FirstOrDefaultAsync(i => i.Id == id);
        if (incidente is null)
            return ResultadoServicio<IncidenteDto>.NoEncontrado();

        var actual = incidente.Estado;

        if (actual == EstadoIncidente.Cerrado)
            return ResultadoServicio<IncidenteDto>.Invalido(
                "El incidente está Cerrado y no se puede reabrir. Si el problema vuelve, crea un incidente nuevo.");

        if (nuevoEstado == EstadoIncidente.Cerrado && actual != EstadoIncidente.Resuelto)
            return ResultadoServicio<IncidenteDto>.Invalido(
                $"No se puede cerrar un incidente que no está Resuelto (estado actual: {actual}).");

        if (actual == EstadoIncidente.Pendiente)
            return ResultadoServicio<IncidenteDto>.Invalido(
                "El incidente está Pendiente: primero un administrador debe asignar un técnico (PUT /api/incidentes/{id}/asignar).");

        var siguiente = actual + 1;
        if (nuevoEstado != siguiente)
            return ResultadoServicio<IncidenteDto>.Invalido(
                $"Transición no permitida: {actual} → {nuevoEstado}. El único estado siguiente válido es {siguiente}.");

        incidente.Estado = nuevoEstado;
        if (nuevoEstado == EstadoIncidente.Resuelto)
            incidente.FechaResolucion = DateTime.UtcNow;

        await db.SaveChangesAsync();
        logger.LogInformation("Incidente {IncidenteId}: {Desde} → {Hacia} por {ActorId}", id, actual, nuevoEstado, actor.Id);
        return ResultadoServicio<IncidenteDto>.Ok(IncidenteDto.FromEntity(incidente));
    }

    public async Task<ResultadoServicio<bool>> EliminarAsync(Actor actor, int id)
    {
        if (actor.Rol != Rol.Administrador)
            return ResultadoServicio<bool>.Prohibido();

        var incidente = await db.Incidentes.FirstOrDefaultAsync(i => i.Id == id);
        if (incidente is null)
            return ResultadoServicio<bool>.NoEncontrado();

        if (incidente.Estado == EstadoIncidente.Cerrado)
            return ResultadoServicio<bool>.Invalido(
                "No se puede eliminar un incidente Cerrado: forma parte del historial del servicio.");

        db.Incidentes.Remove(incidente);
        await db.SaveChangesAsync();
        logger.LogWarning("Incidente {IncidenteId} eliminado por administrador {ActorId}", id, actor.Id);
        return ResultadoServicio<bool>.Ok(true);
    }

    private IQueryable<Incidente> Visibles(Actor actor)
    {
        var query = db.Incidentes.Include(i => i.Usuario).Include(i => i.Tecnico).AsQueryable();

        return actor.Rol switch
        {
            Rol.Administrador => query,
            Rol.Tecnico => query.Where(i => i.TecnicoId == actor.Id),
            Rol.Usuario => query.Where(i => i.UsuarioId == actor.Id),
            _ => query.Where(_ => false)
        };
    }
}
