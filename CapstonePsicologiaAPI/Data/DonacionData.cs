using CapstonePsicologiaAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace CapstonePsicologiaAPI.Data;

public interface IDonacionData
{
    Task CrearAsync(Donacion donacion);
    Task GuardarAsync();
    Task<Donacion?> ObtenerPorIdAsync(int id);
    Task<List<Donacion>> ListarAsync(string? estado, string? buscar);
    Task<Dictionary<string, int>> ContarPorEstadoAsync();
}

public class DonacionData : IDonacionData
{
    private readonly AppDbContext _db;

    public DonacionData(AppDbContext db)
    {
        _db = db;
    }

    public async Task CrearAsync(Donacion donacion)
    {
        _db.Donaciones.Add(donacion);
        await _db.SaveChangesAsync();
    }

    public Task GuardarAsync() => _db.SaveChangesAsync();

    public Task<Donacion?> ObtenerPorIdAsync(int id) =>
        _db.Donaciones.Include(d => d.Articulos).FirstOrDefaultAsync(d => d.Id == id);

    public Task<List<Donacion>> ListarAsync(string? estado, string? buscar)
    {
        var consulta = _db.Donaciones.AsNoTracking().Include(d => d.Articulos).AsQueryable();

        if (!string.IsNullOrWhiteSpace(estado))
            consulta = consulta.Where(d => d.Estado == estado);

        if (!string.IsNullOrWhiteSpace(buscar))
        {
            var texto = buscar.Trim();
            consulta = consulta.Where(d => d.Codigo.Contains(texto) || d.DonanteNombre.Contains(texto)
                                           || d.Articulos.Any(a => a.Nombre.Contains(texto)));
        }

        return consulta.OrderByDescending(d => d.FechaRegistro).ToListAsync();
    }

    public async Task<Dictionary<string, int>> ContarPorEstadoAsync() =>
        await _db.Donaciones.GroupBy(d => d.Estado).Select(g => new { g.Key, Total = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Total);
}

public interface INoticiaData
{
    Task CrearAsync(Noticia noticia);
    Task GuardarAsync();
    Task<Noticia?> ObtenerPorIdAsync(int id);
    Task<List<Noticia>> ListarAsync(bool soloPublicadas, int? maximo);
    Task EliminarAsync(Noticia noticia);
}

public class NoticiaData : INoticiaData
{
    private readonly AppDbContext _db;

    public NoticiaData(AppDbContext db)
    {
        _db = db;
    }

    public async Task CrearAsync(Noticia noticia)
    {
        _db.Noticias.Add(noticia);
        await _db.SaveChangesAsync();
    }

    public Task GuardarAsync() => _db.SaveChangesAsync();

    public Task<Noticia?> ObtenerPorIdAsync(int id) => _db.Noticias.FirstOrDefaultAsync(n => n.Id == id);

    public Task<List<Noticia>> ListarAsync(bool soloPublicadas, int? maximo)
    {
        var consulta = _db.Noticias.AsNoTracking().AsQueryable();

        if (soloPublicadas)
            consulta = consulta.Where(n => n.Estado == EstadosNoticia.Publicada);

        consulta = consulta.OrderByDescending(n => n.Fecha);

        if (maximo is not null)
            consulta = consulta.Take(maximo.Value);

        return consulta.ToListAsync();
    }

    public async Task EliminarAsync(Noticia noticia)
    {
        _db.Noticias.Remove(noticia);
        await _db.SaveChangesAsync();
    }
}
