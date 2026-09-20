using CapstonePsicologiaAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace CapstonePsicologiaAPI.Data;

/// <summary>Acceso a datos de las solicitudes de orientación (tabla Solicitudes).</summary>
public class SolicitudData : ISolicitudData
{
    private readonly AppDbContext _db;

    public SolicitudData(AppDbContext db)
    {
        _db = db;
    }

    private IQueryable<SolicitudOrientacion> ConDatos() =>
        _db.Solicitudes.Include(s => s.Usuario).Include(s => s.PsicologoAsignado);

    public async Task CrearAsync(SolicitudOrientacion solicitud)
    {
        _db.Solicitudes.Add(solicitud);
        await _db.SaveChangesAsync();
    }

    public Task<bool> TieneAbiertaAsync(int usuarioId)
    {
        return _db.Solicitudes.AnyAsync(s => s.UsuarioId == usuarioId && s.Estado != EstadosSolicitud.Cerrada);
    }

    public Task<SolicitudOrientacion?> ObtenerPorIdAsync(int id)
    {
        return ConDatos().AsNoTracking().FirstOrDefaultAsync(s => s.Id == id);
    }

    public Task<SolicitudOrientacion?> ObtenerUltimaDeUsuarioAsync(int usuarioId)
    {
        return ConDatos()
            .AsNoTracking()
            .Where(s => s.UsuarioId == usuarioId)
            .OrderByDescending(s => s.FechaCreacion)
            .FirstOrDefaultAsync();
    }

    public Task<List<SolicitudOrientacion>> ObtenerDeUsuarioAsync(int usuarioId)
    {
        return ConDatos()
            .AsNoTracking()
            .Where(s => s.UsuarioId == usuarioId)
            .OrderByDescending(s => s.FechaCreacion)
            .ToListAsync();
    }

    public Task<List<SolicitudOrientacion>> ObtenerTodasAsync()
    {
        return ConDatos()
            .AsNoTracking()
            .OrderByDescending(s => s.FechaCreacion)
            .ToListAsync();
    }

    public Task<List<SolicitudOrientacion>> ObtenerAsignadasAbiertasAsync(int psicologoId)
    {
        return ConDatos()
            .AsNoTracking()
            .Where(s => s.PsicologoAsignadoId == psicologoId && s.Estado != EstadosSolicitud.Cerrada)
            .OrderByDescending(s => s.FechaActualizacion)
            .ToListAsync();
    }

    public async Task<Dictionary<int, int>> ContarAsignadasAbiertasAsync(int[] psicologoIds)
    {
        return await _db.Solicitudes
            .AsNoTracking()
            .Where(s => s.PsicologoAsignadoId != null && psicologoIds.Contains(s.PsicologoAsignadoId.Value))
            .Where(s => s.Estado != EstadosSolicitud.Cerrada)
            .GroupBy(s => s.PsicologoAsignadoId!.Value)
            .Select(g => new { Id = g.Key, Total = g.Count() })
            .ToDictionaryAsync(x => x.Id, x => x.Total);
    }

    public async Task<SolicitudOrientacion?> CambiarEstadoAsync(int id, string estado, int? psicologoId)
    {
        var solicitud = await ConDatos().FirstOrDefaultAsync(s => s.Id == id);

        if (solicitud is null)
            return null;

        solicitud.Estado = estado;
        solicitud.FechaActualizacion = DateTime.UtcNow;

        if (psicologoId is not null)
            solicitud.PsicologoAsignadoId = psicologoId;

        await _db.SaveChangesAsync();

        // Se recarga la navegación por si el psicólogo asignado cambió.
        await _db.Entry(solicitud).Reference(s => s.PsicologoAsignado).LoadAsync();
        return solicitud;
    }
}
