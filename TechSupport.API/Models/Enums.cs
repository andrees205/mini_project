namespace TechSupport.API.Models;

public enum Rol
{
    Administrador,
    Tecnico,
    Usuario
}

public enum Prioridad
{
    Baja,
    Media,
    Alta,
    Critica
}

public enum EstadoIncidente
{
    Pendiente = 0,
    Asignado = 1,
    EnProceso = 2,
    Resuelto = 3,
    Cerrado = 4
}
