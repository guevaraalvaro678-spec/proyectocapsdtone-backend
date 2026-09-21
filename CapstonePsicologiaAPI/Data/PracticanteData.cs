using CapstonePsicologiaAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace CapstonePsicologiaAPI.Data;

public interface IPracticanteData
{
    Task CrearAsync(Practicante practicante);
    Task<Practicante?> ObtenerPorUsuarioIdAsync(int usuarioId);
    Task<List<Practicante>> ListarAsync(string? estadoHorario, string? buscar);
    Task<List<BloqueHorario>> ObtenerBloquesAsync(int usuarioId);
    Task<List<BloqueHorario>> ObtenerBloquesDeAsync(int[] usuarioIds);
    Task ReemplazarBloquesAsync(int usuarioId, string tipo, IEnumerable<BloqueHorario> bloques);
    Task GuardarAsync();
}

public class PracticanteData : IPracticanteData
{
    private readonly AppDbContext _db;

    public PracticanteData(AppDbContext db)
    {
        _db = db;
    }

    public async Task CrearAsync(Practicante practicante)
    {
        _db.Practicantes.Add(practicante);
        await _db.SaveChangesAsync();
    }

    public Task<Practicante?> ObtenerPorUsuarioIdAsync(int usuarioId) =>
        _db.Practicantes.Include(p => p.Usuario).FirstOrDefaultAsync(p => p.UsuarioId == usuarioId);

    public Task<List<Practicante>> ListarAsync(string? estadoHorario, string? buscar)
    {
        var consulta = _db.Practicantes.AsNoTracking().Include(p => p.Usuario).AsQueryable();

        if (!string.IsNullOrWhiteSpace(estadoHorario))
            consulta = consulta.Where(p => p.EstadoHorario == estadoHorario);

        if (!string.IsNullOrWhiteSpace(buscar))
        {
            var texto = buscar.Trim();
            consulta = consulta.Where(p => p.Dni.Contains(texto) || p.Apellidos.Contains(texto)
                                           || p.Usuario!.Nombre.Contains(texto) || p.Universidad.Contains(texto));
        }

        // Los que esperan revisión van primero.
        return consulta
            .OrderBy(p => p.EstadoHorario == EstadosHorario.Pendiente ? 0 : p.EstadoHorario == EstadosHorario.EnRevision ? 1 : 2)
            .ThenBy(p => p.FechaRegistro)
            .ToListAsync();
    }

    public Task<List<BloqueHorario>> ObtenerBloquesAsync(int usuarioId) =>
        _db.BloquesHorario.AsNoTracking()
            .Where(b => b.UsuarioId == usuarioId)
            .OrderBy(b => b.DiaSemana).ThenBy(b => b.HoraInicio)
            .ToListAsync();

    public Task<List<BloqueHorario>> ObtenerBloquesDeAsync(int[] usuarioIds) =>
        _db.BloquesHorario.AsNoTracking().Where(b => usuarioIds.Contains(b.UsuarioId)).ToListAsync();

    public async Task ReemplazarBloquesAsync(int usuarioId, string tipo, IEnumerable<BloqueHorario> bloques)
    {
        var actuales = _db.BloquesHorario.Where(b => b.UsuarioId == usuarioId && b.Tipo == tipo);
        _db.BloquesHorario.RemoveRange(actuales);
        _db.BloquesHorario.AddRange(bloques);
        await _db.SaveChangesAsync();
    }

    public Task GuardarAsync() => _db.SaveChangesAsync();
}
