using CapstonePsicologiaAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace CapstonePsicologiaAPI.Data;

public interface IMarcacionData
{
    Task<Marcacion?> ObtenerAbiertaAsync(int usuarioId);
    Task CrearAsync(Marcacion marcacion);
    Task GuardarAsync();
    Task<List<Marcacion>> ListarAsync(int? usuarioId, DateTime? desdeUtc, int maximo);
}

public class MarcacionData : IMarcacionData
{
    private readonly AppDbContext _db;

    public MarcacionData(AppDbContext db)
    {
        _db = db;
    }

    public Task<Marcacion?> ObtenerAbiertaAsync(int usuarioId) =>
        _db.Marcaciones.Include(m => m.Usuario)
            .Where(m => m.UsuarioId == usuarioId && m.Salida == null)
            .OrderByDescending(m => m.Entrada)
            .FirstOrDefaultAsync();

    public async Task CrearAsync(Marcacion marcacion)
    {
        _db.Marcaciones.Add(marcacion);
        await _db.SaveChangesAsync();
    }

    public Task GuardarAsync() => _db.SaveChangesAsync();

    public Task<List<Marcacion>> ListarAsync(int? usuarioId, DateTime? desdeUtc, int maximo)
    {
        var consulta = _db.Marcaciones.AsNoTracking().Include(m => m.Usuario).AsQueryable();

        if (usuarioId is not null)
            consulta = consulta.Where(m => m.UsuarioId == usuarioId);

        if (desdeUtc is not null)
            consulta = consulta.Where(m => m.Entrada >= desdeUtc);

        return consulta.OrderByDescending(m => m.Entrada).Take(maximo).ToListAsync();
    }
}
