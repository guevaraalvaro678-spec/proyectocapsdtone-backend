using CapstonePsicologiaAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace CapstonePsicologiaAPI.Data;

public interface ISitioData
{
    Task<Imagen> CrearImagenAsync(Imagen imagen);
    Task<Imagen?> ObtenerImagenAsync(int id);
    Task EliminarImagenAsync(int? id);

    Task<SitioConfig> ObtenerConfigAsync();
    Task GuardarAsync();

    Task<List<Iniciativa>> ListarIniciativasAsync(bool soloActivas);
    Task<Iniciativa?> ObtenerIniciativaAsync(int id);
    Task CrearIniciativaAsync(Iniciativa iniciativa);
    Task EliminarIniciativaAsync(Iniciativa iniciativa);
}

public class SitioData : ISitioData
{
    private readonly AppDbContext _db;

    public SitioData(AppDbContext db)
    {
        _db = db;
    }

    public async Task<Imagen> CrearImagenAsync(Imagen imagen)
    {
        _db.Imagenes.Add(imagen);
        await _db.SaveChangesAsync();
        return imagen;
    }

    public Task<Imagen?> ObtenerImagenAsync(int id) =>
        _db.Imagenes.AsNoTracking().FirstOrDefaultAsync(i => i.Id == id);

    /// <summary>Borra la imagen si ya nada la usa (logo o iniciativas).</summary>
    public async Task EliminarImagenAsync(int? id)
    {
        if (id is null)
            return;

        var enUso = await _db.Iniciativas.AnyAsync(i => i.ImagenId == id)
                    || await _db.SitioConfig.AnyAsync(c => c.LogoImagenId == id);

        if (!enUso)
            await _db.Imagenes.Where(i => i.Id == id).ExecuteDeleteAsync();
    }

    public async Task<SitioConfig> ObtenerConfigAsync()
    {
        var config = await _db.SitioConfig.FirstOrDefaultAsync();

        if (config is null)
        {
            config = new SitioConfig();
            _db.SitioConfig.Add(config);
            await _db.SaveChangesAsync();
        }

        return config;
    }

    public Task GuardarAsync() => _db.SaveChangesAsync();

    public Task<List<Iniciativa>> ListarIniciativasAsync(bool soloActivas)
    {
        var consulta = _db.Iniciativas.AsNoTracking().AsQueryable();

        if (soloActivas)
            consulta = consulta.Where(i => i.Activa);

        return consulta.OrderBy(i => i.Orden).ThenBy(i => i.Id).ToListAsync();
    }

    public Task<Iniciativa?> ObtenerIniciativaAsync(int id) =>
        _db.Iniciativas.FirstOrDefaultAsync(i => i.Id == id);

    public async Task CrearIniciativaAsync(Iniciativa iniciativa)
    {
        _db.Iniciativas.Add(iniciativa);
        await _db.SaveChangesAsync();
    }

    public async Task EliminarIniciativaAsync(Iniciativa iniciativa)
    {
        _db.Iniciativas.Remove(iniciativa);
        await _db.SaveChangesAsync();
    }
}
