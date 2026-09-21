using CapstonePsicologiaAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace CapstonePsicologiaAPI.Data;

public interface ITareaData
{
    Task CrearAsync(Tarea tarea);
    Task<Tarea?> ObtenerPorIdAsync(int id);
    Task GuardarAsync();
    Task<List<Tarea>> ListarAsync(int? usuarioId);
}

public class TareaData : ITareaData
{
    private readonly AppDbContext _db;

    public TareaData(AppDbContext db)
    {
        _db = db;
    }

    public async Task CrearAsync(Tarea tarea)
    {
        _db.Tareas.Add(tarea);
        await _db.SaveChangesAsync();
    }

    public Task<Tarea?> ObtenerPorIdAsync(int id) =>
        _db.Tareas.Include(t => t.Usuario).FirstOrDefaultAsync(t => t.Id == id);

    public Task GuardarAsync() => _db.SaveChangesAsync();

    /// <summary>Sin `usuarioId` devuelve las de todos. Pendientes primero, urgentes arriba.</summary>
    public Task<List<Tarea>> ListarAsync(int? usuarioId)
    {
        var consulta = _db.Tareas.AsNoTracking().Include(t => t.Usuario).AsQueryable();

        if (usuarioId is not null)
            consulta = consulta.Where(t => t.UsuarioId == usuarioId);

        return consulta
            .OrderBy(t => t.Estado == EstadosTarea.Pendiente ? 0 : 1)
            .ThenByDescending(t => t.Urgente)
            .ThenBy(t => t.FechaLimite == null)
            .ThenBy(t => t.FechaLimite)
            .ThenByDescending(t => t.Id)
            .ToListAsync();
    }
}
