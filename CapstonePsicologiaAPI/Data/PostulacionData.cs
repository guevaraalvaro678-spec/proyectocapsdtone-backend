using CapstonePsicologiaAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace CapstonePsicologiaAPI.Data;

public interface IPostulacionData
{
    Task CrearAsync(Postulacion postulacion);
    Task GuardarAsync();
    Task<Postulacion?> ObtenerPorIdAsync(int id);

    /// <summary>La postulación más reciente de una cuenta.</summary>
    Task<Postulacion?> ObtenerUltimaDeUsuarioAsync(int usuarioId);

    Task<List<Postulacion>> ListarAsync(string? tipo, string? estado, string? buscar);
    Task<List<Postulacion>> ListarDeSesionAsync(int sesionId);
    Task<Dictionary<string, int>> ContarPorEstadoAsync();
    Task<bool> DniEnUsoAsync(string dni, int usuarioId);
}

public class PostulacionData : IPostulacionData
{
    private readonly AppDbContext _db;

    public PostulacionData(AppDbContext db)
    {
        _db = db;
    }

    private IQueryable<Postulacion> ConRelaciones() =>
        _db.Postulaciones.Include(p => p.Usuario).Include(p => p.SesionEntrevista).Include(p => p.CvArchivo);

    public async Task CrearAsync(Postulacion postulacion)
    {
        _db.Postulaciones.Add(postulacion);
        await _db.SaveChangesAsync();
    }

    public Task GuardarAsync() => _db.SaveChangesAsync();

    // El CV se incluye solo para saber su nombre; los bytes no se cargan en las listas (ver Select en ListarAsync).
    public Task<Postulacion?> ObtenerPorIdAsync(int id) =>
        _db.Postulaciones.Include(p => p.Usuario).Include(p => p.SesionEntrevista).FirstOrDefaultAsync(p => p.Id == id);

    public Task<Postulacion?> ObtenerUltimaDeUsuarioAsync(int usuarioId) =>
        _db.Postulaciones.Include(p => p.Usuario).Include(p => p.SesionEntrevista)
            .Where(p => p.UsuarioId == usuarioId)
            .OrderByDescending(p => p.FechaEnvio)
            .FirstOrDefaultAsync();

    public Task<List<Postulacion>> ListarAsync(string? tipo, string? estado, string? buscar)
    {
        var consulta = _db.Postulaciones.AsNoTracking().Include(p => p.Usuario).Include(p => p.SesionEntrevista).AsQueryable();

        if (!string.IsNullOrWhiteSpace(tipo))
            consulta = consulta.Where(p => p.Tipo == tipo);

        if (!string.IsNullOrWhiteSpace(estado))
            consulta = consulta.Where(p => p.Estado == estado);

        if (!string.IsNullOrWhiteSpace(buscar))
        {
            var texto = buscar.Trim();
            consulta = consulta.Where(p => p.Codigo.Contains(texto) || p.Nombres.Contains(texto) || p.Apellidos.Contains(texto)
                                           || p.Dni.Contains(texto) || p.Usuario!.Correo!.Contains(texto));
        }

        return consulta.OrderByDescending(p => p.FechaEnvio).ToListAsync();
    }

    public Task<List<Postulacion>> ListarDeSesionAsync(int sesionId) =>
        _db.Postulaciones.Include(p => p.Usuario).Include(p => p.SesionEntrevista)
            .Where(p => p.SesionEntrevistaId == sesionId)
            .OrderBy(p => p.Apellidos)
            .ToListAsync();

    public async Task<Dictionary<string, int>> ContarPorEstadoAsync() =>
        await _db.Postulaciones.GroupBy(p => p.Estado).Select(g => new { g.Key, Total = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Total);

    /// <summary>El DNI no puede estar en un perfil de miembro ni en la postulación de otra cuenta.</summary>
    public async Task<bool> DniEnUsoAsync(string dni, int usuarioId) =>
        await _db.Voluntarios.AnyAsync(v => v.Dni == dni)
        || await _db.Practicantes.AnyAsync(p => p.Dni == dni)
        || await _db.Postulaciones.AnyAsync(p => p.Dni == dni && p.UsuarioId != usuarioId && p.Estado != EstadosPostulacion.NoAdmitida);
}

public interface ISesionEntrevistaData
{
    Task CrearAsync(SesionEntrevista sesion);
    Task GuardarAsync();
    Task<SesionEntrevista?> ObtenerPorIdAsync(int id);
    Task<List<SesionEntrevista>> ListarAsync(string? estado);
    Task<List<SesionEntrevista>> ProximasAsync(DateTime desdeUtc, int maximo);

    /// <summary>Cuántos postulantes tiene asignados cada sesión (clave = Id de la sesión).</summary>
    Task<Dictionary<int, int>> ContarInscritosAsync(int[] sesionIds);
}

public class SesionEntrevistaData : ISesionEntrevistaData
{
    private readonly AppDbContext _db;

    public SesionEntrevistaData(AppDbContext db)
    {
        _db = db;
    }

    public async Task CrearAsync(SesionEntrevista sesion)
    {
        _db.SesionesEntrevista.Add(sesion);
        await _db.SaveChangesAsync();
    }

    public Task GuardarAsync() => _db.SaveChangesAsync();

    public Task<SesionEntrevista?> ObtenerPorIdAsync(int id) =>
        _db.SesionesEntrevista.FirstOrDefaultAsync(s => s.Id == id);

    public Task<List<SesionEntrevista>> ListarAsync(string? estado)
    {
        var consulta = _db.SesionesEntrevista.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(estado))
            consulta = consulta.Where(s => s.Estado == estado);

        // Las programadas primero (la más cercana arriba), luego el resto de la más reciente a la más antigua.
        return consulta.OrderBy(s => s.Estado == EstadosSesion.Programada ? 0 : 1)
            .ThenBy(s => s.Estado == EstadosSesion.Programada ? s.Inicio : DateTime.MaxValue)
            .ThenByDescending(s => s.Inicio)
            .ToListAsync();
    }

    public Task<List<SesionEntrevista>> ProximasAsync(DateTime desdeUtc, int maximo) =>
        _db.SesionesEntrevista.AsNoTracking()
            .Where(s => s.Estado == EstadosSesion.Programada && s.Fin >= desdeUtc)
            .OrderBy(s => s.Inicio)
            .Take(maximo)
            .ToListAsync();

    public async Task<Dictionary<int, int>> ContarInscritosAsync(int[] sesionIds) =>
        await _db.Postulaciones
            .Where(p => p.SesionEntrevistaId != null && sesionIds.Contains(p.SesionEntrevistaId.Value))
            .GroupBy(p => p.SesionEntrevistaId!.Value)
            .Select(g => new { g.Key, Total = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Total);
}

public interface IArchivoData
{
    Task<Archivo> CrearAsync(Archivo archivo);
    Task<Archivo?> ObtenerAsync(int id);

    /// <summary>Nombre del archivo sin cargar su contenido.</summary>
    Task<string?> ObtenerNombreAsync(int id);
}

public class ArchivoData : IArchivoData
{
    private readonly AppDbContext _db;

    public ArchivoData(AppDbContext db)
    {
        _db = db;
    }

    public async Task<Archivo> CrearAsync(Archivo archivo)
    {
        _db.Archivos.Add(archivo);
        await _db.SaveChangesAsync();
        return archivo;
    }

    public Task<Archivo?> ObtenerAsync(int id) =>
        _db.Archivos.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id);

    public Task<string?> ObtenerNombreAsync(int id) =>
        _db.Archivos.Where(a => a.Id == id).Select(a => a.Nombre).FirstOrDefaultAsync();
}
