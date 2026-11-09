using TechSupport.API.Models;

namespace TechSupport.API.Data;

public static class DbSeeder
{
    public static void Seed(AppDbContext db)
    {
        if (db.Usuarios.Any()) return;

        db.Usuarios.AddRange(
            Crear("admin", "Admin123!", Rol.Administrador),
            Crear("tecnico1", "Tecnico123!", Rol.Tecnico),
            Crear("tecnico2", "Tecnico123!", Rol.Tecnico),
            Crear("usuario1", "Usuario123!", Rol.Usuario),
            Crear("usuario2", "Usuario123!", Rol.Usuario));

        db.SaveChanges();
    }

    private static Usuario Crear(string nombre, string password, Rol rol) => new()
    {
        NombreUsuario = nombre,
        PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
        Rol = rol
    };
}
