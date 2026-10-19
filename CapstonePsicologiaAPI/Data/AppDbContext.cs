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
    public DbSet<Voluntario> Voluntarios => Set<Voluntario>();
    public DbSet<Practicante> Practicantes => Set<Practicante>();
    public DbSet<BloqueHorario> BloquesHorario => Set<BloqueHorario>();
    public DbSet<Tarea> Tareas => Set<Tarea>();
    public DbSet<Marcacion> Marcaciones => Set<Marcacion>();
    public DbSet<Imagen> Imagenes => Set<Imagen>();
    public DbSet<Iniciativa> Iniciativas => Set<Iniciativa>();
    public DbSet<SitioConfig> SitioConfig => Set<SitioConfig>();
    public DbSet<Postulacion> Postulaciones => Set<Postulacion>();
    public DbSet<SesionEntrevista> SesionesEntrevista => Set<SesionEntrevista>();
    public DbSet<HistorialPostulacion> HistorialPostulaciones => Set<HistorialPostulacion>();
    public DbSet<Archivo> Archivos => Set<Archivo>();
    public DbSet<Donacion> Donaciones => Set<Donacion>();
    public DbSet<DonacionArticulo> DonacionArticulos => Set<DonacionArticulo>();
    public DbSet<Noticia> Noticias => Set<Noticia>();

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
            e.Property(x => x.Tipo).HasMaxLength(20).IsRequired().HasDefaultValue(TiposEvento.Evento);
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
            e.Property(p => p.Dni).HasMaxLength(8).IsFixedLength();
            e.HasIndex(p => p.Dni).IsUnique().HasFilter("[Dni] IS NOT NULL");
        });

        modelBuilder.Entity<Voluntario>(e =>
        {
            // La clave es la cuenta: el voluntario puede no tener DNI registrado.
            e.HasKey(v => v.UsuarioId);
            e.Property(v => v.Dni).HasMaxLength(8).IsFixedLength();
            e.HasIndex(v => v.Dni).IsUnique().HasFilter("[Dni] IS NOT NULL");
            e.Property(v => v.Apellidos).HasMaxLength(100).IsRequired();
            e.HasOne(v => v.Usuario).WithMany().HasForeignKey(v => v.UsuarioId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Practicante>(e =>
        {
            e.HasKey(p => p.Dni);
            e.Property(p => p.Dni).HasMaxLength(8).IsFixedLength();
            e.Property(p => p.Apellidos).HasMaxLength(100).IsRequired();
            e.Property(p => p.Universidad).HasMaxLength(150).IsRequired();
            e.Property(p => p.Carrera).HasMaxLength(100).IsRequired();
            e.Property(p => p.Ciclo).HasMaxLength(30).IsRequired();
            e.Property(p => p.DepartamentoPrincipal).HasMaxLength(100).IsRequired();
            e.Property(p => p.DepartamentoSecundario).HasMaxLength(100).IsRequired();
            e.Property(p => p.EstadoHorario).HasMaxLength(20).IsRequired();
            e.Property(p => p.NotaHorario).HasMaxLength(500);
            e.HasOne(p => p.Usuario).WithMany().HasForeignKey(p => p.UsuarioId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(p => p.UsuarioId).IsUnique();
            e.HasIndex(p => p.EstadoHorario);
        });

        modelBuilder.Entity<BloqueHorario>(e =>
        {
            e.Property(b => b.Tipo).HasMaxLength(20).IsRequired();
            e.Property(b => b.HoraInicio).HasMaxLength(5).IsRequired();
            e.Property(b => b.HoraFin).HasMaxLength(5).IsRequired();
            e.Property(b => b.Modalidad).HasMaxLength(20);
            e.Property(b => b.Lugar).HasMaxLength(200);
            e.HasOne(b => b.Usuario).WithMany().HasForeignKey(b => b.UsuarioId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(b => new { b.UsuarioId, b.Tipo });
        });

        modelBuilder.Entity<Tarea>(e =>
        {
            e.Property(t => t.Titulo).HasMaxLength(150).IsRequired();
            e.Property(t => t.Descripcion).HasMaxLength(1000);
            e.Property(t => t.Estado).HasMaxLength(20).IsRequired();
            e.HasOne(t => t.Usuario).WithMany().HasForeignKey(t => t.UsuarioId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(t => new { t.UsuarioId, t.Estado });
        });

        modelBuilder.Entity<Marcacion>(e =>
        {
            e.HasOne(m => m.Usuario).WithMany().HasForeignKey(m => m.UsuarioId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(m => new { m.UsuarioId, m.Entrada });
        });

        modelBuilder.Entity<Imagen>(e =>
        {
            e.Property(i => i.Nombre).HasMaxLength(200).IsRequired();
            e.Property(i => i.TipoContenido).HasMaxLength(50).IsRequired();
        });

        modelBuilder.Entity<Iniciativa>(e =>
        {
            e.Property(i => i.Titulo).HasMaxLength(150).IsRequired();
            e.Property(i => i.Texto).HasMaxLength(600).IsRequired();
            e.Property(i => i.Icono).HasMaxLength(10).IsRequired();
            e.Property(i => i.Color).HasMaxLength(10).IsRequired();
            e.Property(i => i.Estado).HasMaxLength(20).IsRequired().HasDefaultValue(EstadosIniciativa.Aprobada);
            e.Property(i => i.MotivoRechazo).HasMaxLength(500);
            e.HasOne(i => i.CreadaPor).WithMany().HasForeignKey(i => i.CreadaPorUsuarioId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(i => i.Imagen).WithMany().HasForeignKey(i => i.ImagenId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<SitioConfig>(e =>
        {
            e.Property(c => c.Id).ValueGeneratedNever();
            e.HasOne(c => c.LogoImagen).WithMany().HasForeignKey(c => c.LogoImagenId).OnDelete(DeleteBehavior.SetNull);
            // NoAction: SQL Server no permite dos borrados en cascada hacia la misma tabla (el servicio borra la imagen anterior).
            e.HasOne(c => c.YapeQrImagen).WithMany().HasForeignKey(c => c.YapeQrImagenId).OnDelete(DeleteBehavior.NoAction);
            e.Property(c => c.YapeNumero).HasMaxLength(9);
            e.Property(c => c.YapeTitular).HasMaxLength(100);
        });

        modelBuilder.Entity<Postulacion>(e =>
        {
            e.Property(p => p.Codigo).HasMaxLength(20).IsRequired();
            e.HasIndex(p => p.Codigo).IsUnique();
            e.Property(p => p.Tipo).HasMaxLength(20).IsRequired();
            // DNI, nacimiento, dirección y área solo se piden a practicantes.
            e.Property(p => p.Dni).HasMaxLength(8).IsFixedLength();
            e.Property(p => p.Nombres).HasMaxLength(100).IsRequired();
            e.Property(p => p.Apellidos).HasMaxLength(100).IsRequired();
            e.Property(p => p.Direccion).HasMaxLength(200);
            e.Property(p => p.Distrito).HasMaxLength(100);
            e.Property(p => p.Provincia).HasMaxLength(100);
            e.Property(p => p.Telefono).HasMaxLength(30).IsRequired();
            e.Property(p => p.Universidad).HasMaxLength(150);
            e.Property(p => p.Carrera).HasMaxLength(100);
            e.Property(p => p.Ciclo).HasMaxLength(30);
            e.Property(p => p.Departamento).HasMaxLength(100);
            e.Property(p => p.DepartamentoSecundario).HasMaxLength(100);
            e.Property(p => p.IniciativaDescripcion).HasMaxLength(600);
            e.Property(p => p.Estado).HasMaxLength(30).IsRequired();
            e.Property(p => p.NotaEvaluacion).HasMaxLength(600);
            e.HasOne(p => p.Usuario).WithMany().HasForeignKey(p => p.UsuarioId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(p => p.CvArchivo).WithMany().HasForeignKey(p => p.CvArchivoId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(p => p.SesionEntrevista).WithMany().HasForeignKey(p => p.SesionEntrevistaId).OnDelete(DeleteBehavior.SetNull);
            e.HasIndex(p => p.Estado);
            e.HasIndex(p => p.UsuarioId);
        });

        modelBuilder.Entity<HistorialPostulacion>(e =>
        {
            e.Property(h => h.Estado).HasMaxLength(30).IsRequired();
            e.Property(h => h.Nota).HasMaxLength(600);
            e.HasOne<Postulacion>().WithMany().HasForeignKey(h => h.PostulacionId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(h => h.Usuario).WithMany().HasForeignKey(h => h.UsuarioId).OnDelete(DeleteBehavior.NoAction);
            e.HasIndex(h => h.PostulacionId);
        });

        modelBuilder.Entity<SesionEntrevista>(e =>
        {
            e.Property(s => s.Codigo).HasMaxLength(20).IsRequired();
            e.HasIndex(s => s.Codigo).IsUnique();
            e.Property(s => s.Nombre).HasMaxLength(150).IsRequired();
            e.Property(s => s.Modalidad).HasMaxLength(20).IsRequired();
            e.Property(s => s.LugarOEnlace).HasMaxLength(300).IsRequired();
            e.Property(s => s.Estado).HasMaxLength(20).IsRequired();
            e.HasIndex(s => s.Inicio);
        });

        modelBuilder.Entity<Archivo>(e =>
        {
            e.Property(a => a.Nombre).HasMaxLength(200).IsRequired();
            e.Property(a => a.TipoContenido).HasMaxLength(100).IsRequired();
        });

        modelBuilder.Entity<Donacion>(e =>
        {
            e.Property(d => d.Codigo).HasMaxLength(20).IsRequired();
            e.HasIndex(d => d.Codigo).IsUnique();
            e.Property(d => d.DonanteNombre).HasMaxLength(150).IsRequired();
            e.Property(d => d.Telefono).HasMaxLength(30).IsRequired();
            e.Property(d => d.Estado).HasMaxLength(30).IsRequired();
            e.Property(d => d.Observaciones).HasMaxLength(500);
            e.HasMany(d => d.Articulos).WithOne().HasForeignKey(a => a.DonacionId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(d => d.Estado);
        });

        modelBuilder.Entity<DonacionArticulo>(e =>
        {
            e.Property(a => a.Categoria).HasMaxLength(30).IsRequired();
            e.Property(a => a.Nombre).HasMaxLength(150).IsRequired();
        });

        modelBuilder.Entity<Noticia>(e =>
        {
            e.Property(n => n.Titulo).HasMaxLength(200).IsRequired();
            e.Property(n => n.Resumen).HasMaxLength(400).IsRequired();
            e.Property(n => n.Contenido).HasMaxLength(4000);
            e.Property(n => n.Estado).HasMaxLength(20).IsRequired();
            e.HasOne(n => n.Imagen).WithMany().HasForeignKey(n => n.ImagenId).OnDelete(DeleteBehavior.SetNull);
            e.HasIndex(n => new { n.Estado, n.Fecha });
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
