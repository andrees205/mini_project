using Microsoft.EntityFrameworkCore;
using TechSupport.API.Models;

namespace TechSupport.API.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Incidente> Incidentes => Set<Incidente>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>();
    }

    private sealed class UtcDateTimeConverter() : Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<DateTime, DateTime>(
        v => v.ToUniversalTime(),
        v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Usuario>(e =>
        {
            e.Property(u => u.NombreUsuario).IsRequired().HasMaxLength(50);
            e.HasIndex(u => u.NombreUsuario).IsUnique();
            e.Property(u => u.PasswordHash).IsRequired();
            e.Property(u => u.Rol).HasConversion<string>().HasMaxLength(20);
        });

        modelBuilder.Entity<Incidente>(e =>
        {
            e.Property(i => i.Titulo).IsRequired().HasMaxLength(150);
            e.Property(i => i.Descripcion).IsRequired().HasMaxLength(2000);
            e.Property(i => i.Prioridad).HasConversion<string>().HasMaxLength(20);
            e.Property(i => i.Estado).HasConversion<string>().HasMaxLength(20);

            e.HasOne(i => i.Usuario)
             .WithMany(u => u.IncidentesReportados)
             .HasForeignKey(i => i.UsuarioId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(i => i.Tecnico)
             .WithMany(u => u.IncidentesAsignados)
             .HasForeignKey(i => i.TecnicoId)
             .IsRequired(false)
             .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
