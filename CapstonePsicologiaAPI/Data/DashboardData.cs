using CapstonePsicologiaAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace CapstonePsicologiaAPI.Data;

/// <summary>Consultas de conteo para el dashboard del administrador.</summary>
public class DashboardData : IDashboardData
{
    private readonly AppDbContext _db;

    public DashboardData(AppDbContext db)
    {
        _db = db;
    }

    public Task<int> ContarUsuariosActivosAsync() =>
        _db.Usuarios.CountAsync(u => u.Rol == Roles.Usuario && u.Activo);

    public Task<int> ContarPsicologosActivosAsync() =>
        _db.Usuarios.CountAsync(u => u.Rol == Roles.Psicologo && u.Activo);

    public Task<int> ContarCitasAsync(DateTime desde, DateTime hasta) =>
        _db.Citas.CountAsync(c =>
            (c.Estado == EstadosCita.Programada || c.Estado == EstadosCita.Confirmada) &&
            c.FechaInicio >= desde && c.FechaInicio < hasta);

    public Task<int> ContarSolicitudesPendientesAsync() =>
        _db.Solicitudes.CountAsync(s => s.Estado == EstadosSolicitud.Pendiente);

    public Task<int> ContarMensajesSinLeerAsync() =>
        _db.Contactos.CountAsync(c => !c.Leido);

    public Task<int> ContarEventosPublicadosAsync() =>
        _db.Eventos.CountAsync(e => e.Publicado && e.FechaInicio >= DateTime.UtcNow.Date);

    public Task<List<DateTime>> ObtenerFechasDeCitasAsync(DateTime desde, DateTime hasta) =>
        _db.Citas
            .AsNoTracking()
            .Where(c => (c.Estado == EstadosCita.Programada || c.Estado == EstadosCita.Confirmada)
                        && c.FechaInicio >= desde && c.FechaInicio < hasta)
            .Select(c => c.FechaInicio)
            .ToListAsync();
}
