using TechSupport.API.Models;

namespace TechSupport.API.Services;

public record Actor(int Id, Rol Rol);

public enum TipoResultado
{
    Ok,
    NoEncontrado,
    Invalido,
    Prohibido
}

public record ResultadoServicio<T>(TipoResultado Tipo, T? Valor = default, string? Mensaje = null)
{
    public static ResultadoServicio<T> Ok(T valor) => new(TipoResultado.Ok, valor);
    public static ResultadoServicio<T> NoEncontrado() => new(TipoResultado.NoEncontrado, Mensaje: "Incidente no encontrado.");
    public static ResultadoServicio<T> Invalido(string mensaje) => new(TipoResultado.Invalido, Mensaje: mensaje);
    public static ResultadoServicio<T> Prohibido() => new(TipoResultado.Prohibido, Mensaje: "No tienes permiso para realizar esta operación.");
}
