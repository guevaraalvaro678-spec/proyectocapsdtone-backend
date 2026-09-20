using CapstonePsicologiaAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace CapstonePsicologiaAPI.Data;

/// <summary>Acceso a datos de los eventos (tabla Eventos).</summary>
public class EventoData : IEventoData
{
    private readonly AppDbContext _db;

    public EventoData(AppDbContext db)
    {
        _db = db;
    }

    public Task<List<Evento>> ObtenerPublicosAsync(DateTime desde, int maximo)
    {
        return _db.Eventos
            .AsNoTracking()
            .Where(e => e.Publicado && e.FechaInicio >= desde)
            .OrderBy(e => e.FechaInicio)
            .Take(maximo)
            .ToListAsync();
    }

    public Task<List<Evento>> ObtenerTodosAsync()
    {
        return _db.Eventos
            .AsNoTracking()
            .OrderByDescending(e => e.FechaInicio)
            .ToListAsync();
    }

    public Task<Evento?> ObtenerPorIdAsync(int id)
    {
        return _db.Eventos.FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task CrearAsync(Evento evento)
    {
        _db.Eventos.Add(evento);
        await _db.SaveChangesAsync();
    }

    public async Task ActualizarAsync(Evento evento)
    {
        _db.Eventos.Update(evento);
        await _db.SaveChangesAsync();
    }

    public async Task EliminarAsync(Evento evento)
    {
        _db.Eventos.Remove(evento);
        await _db.SaveChangesAsync();
    }
}
