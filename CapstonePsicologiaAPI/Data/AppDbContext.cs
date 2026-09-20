using CapstonePsicologiaAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace CapstonePsicologiaAPI.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Contacto> Contactos => Set<Contacto>();
    public DbSet<SolicitudOrientacion> Solicitudes => Set<SolicitudOrientacion>();
    public DbSet<Evento> Eventos => Set<Evento>();
    public DbSet<Cita> Citas => Set<Cita>();
    public DbSet<CorreoSaliente> Correos => Set<CorreoSaliente>();
    public DbSet<Disponibilidad> Disponibilidades => Set<Disponibilidad>();
    public DbSet<Psicologo> Psicologos => Set<Psicologo>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Usuario>(e =>
        {
            e.HasIndex(u => u.NombreUsuario).IsUnique();
            e.HasIndex(u => u.Correo).IsUnique().HasFilter("[Correo] IS NOT NULL");
            e.Property(u => u.NombreUsuario).HasMaxLength(150).IsRequired();
            e.Property(u => u.PasswordHash).IsRequired();
            e.Property(u => u.Rol).HasMaxLength(20).IsRequired();
            e.Property(u => u.Nombre).HasMaxLength(100).IsRequired();
            e.Property(u => u.Correo).HasMaxLength(150);
            e.Property(u => u.Telefono).HasMaxLength(30);
            e.Property(u => u.Ubicacion).HasMaxLength(100);
            e.Property(u => u.Intereses).HasMaxLength(200);
            e.Property(u => u.Especialidad).HasMaxLength(100);
            e.Property(u => u.NumeroColegiatura).HasMaxLength(30);
            e.Property(u => u.TokenRecuperacionHash).HasMaxLength(100);
        });

        modelBuilder.Entity<Contacto>(e =>
        {
            e.Property(c => c.Nombre).HasMaxLength(100).IsRequired();
            e.Property(c => c.Correo).HasMaxLength(150).IsRequired();
            e.Property(c => c.Mensaje).HasMaxLength(1000).IsRequired();
        });

        modelBuilder.Entity<Evento>(e =>
        {
            e.Property(x => x.Titulo).HasMaxLength(150).IsRequired();
            e.Property(x => x.Descripcion).HasMaxLength(600).IsRequired();
            e.Property(x => x.Lugar).HasMaxLength(150).IsRequired();
            e.Property(x => x.Modalidad).HasMaxLength(20).IsRequired();
            e.Property(x => x.ImagenUrl).HasMaxLength(500);
            e.HasIndex(x => x.FechaInicio);
        });

        modelBuilder.Entity<SolicitudOrientacion>(e =>
        {
            e.Property(s => s.Motivo).HasMaxLength(1000).IsRequired();
            e.Property(s => s.Estado).HasMaxLength(20).IsRequired();
            e.HasOne(s => s.Usuario).WithMany().HasForeignKey(s => s.UsuarioId);
            e.HasOne(s => s.PsicologoAsignado)
                .WithMany()
                .HasForeignKey(s => s.PsicologoAsignadoId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Cita>(e =>
        {
            e.Property(c => c.Modalidad).HasMaxLength(20).IsRequired();
            e.Property(c => c.Lugar).HasMaxLength(200);
            e.Property(c => c.Motivo).HasMaxLength(300).IsRequired();
            e.Property(c => c.Estado).HasMaxLength(20).IsRequired();
            e.Property(c => c.Notas).HasMaxLength(2000);

            // Dos relaciones con Usuarios: se restringe el borrado para evitar rutas en cascada duplicadas.
            e.HasOne(c => c.Paciente).WithMany().HasForeignKey(c => c.PacienteId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(c => c.Psicologo).WithMany().HasForeignKey(c => c.PsicologoId).OnDelete(DeleteBehavior.Restrict);

            // Un psicólogo no puede tener dos citas activas que empiecen a la misma hora: además de la
            // validación del servicio, esto frena dos reservas simultáneas del mismo horario.
            e.HasIndex(c => new { c.PsicologoId, c.FechaInicio })
                .IsUnique()
                .HasFilter("[Estado] IN ('Programada', 'Confirmada')");
            e.HasIndex(c => new { c.PacienteId, c.FechaInicio });
        });

        modelBuilder.Entity<Disponibilidad>(e =>
        {
            e.Property(d => d.Modalidad).HasMaxLength(20).IsRequired();
            e.Property(d => d.Lugar).HasMaxLength(200);
            e.HasOne(d => d.Psicologo).WithMany().HasForeignKey(d => d.PsicologoId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(d => new { d.PsicologoId, d.DiaSemana });
        });

        modelBuilder.Entity<Psicologo>(e =>
        {
            e.Property(p => p.Nombre).HasMaxLength(100).IsRequired();
            e.Property(p => p.Correo).HasMaxLength(150).IsRequired();
            e.HasIndex(p => p.Correo).IsUnique();
            e.Property(p => p.PasswordHash).HasMaxLength(500);
            e.Property(p => p.Telefono).HasMaxLength(30);
            e.Property(p => p.Especialidad).HasMaxLength(100).IsRequired();
            e.Property(p => p.NumeroColegiatura).HasMaxLength(30);
            e.Property(p => p.Presentacion).HasMaxLength(1000);
            e.Property(p => p.Estado).HasMaxLength(20).IsRequired();
            e.Property(p => p.MotivoRechazo).HasMaxLength(500);
            e.HasOne(p => p.Usuario).WithMany().HasForeignKey(p => p.UsuarioId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(p => p.Estado);
        });

        modelBuilder.Entity<CorreoSaliente>(e =>
        {
            e.Property(c => c.Destinatario).HasMaxLength(150).IsRequired();
            e.Property(c => c.NombreDestinatario).HasMaxLength(100);
            e.Property(c => c.Asunto).HasMaxLength(200).IsRequired();
            e.Property(c => c.Cuerpo).IsRequired();
            e.Property(c => c.Tipo).HasMaxLength(30).IsRequired();
            e.Property(c => c.Estado).HasMaxLength(20).IsRequired();
            e.Property(c => c.Error).HasMaxLength(500);
            e.Property(c => c.Lote).HasMaxLength(36);
            e.HasIndex(c => new { c.Estado, c.Id });
        });
    }
}
