using CapstonePsicologiaAPI.Data;
using CapstonePsicologiaAPI.DTOs;
using CapstonePsicologiaAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace CapstonePsicologiaAPI.Services;

public interface IAnuncioService
{
    Task<List<AnuncioDto>> ListarVigentesAsync();
    Task<List<AnuncioDto>> ListarAsync();
    Task<AnuncioDto> CrearAsync(AnuncioGuardarDto datos);
    Task<AnuncioDto> ActualizarAsync(int id, AnuncioGuardarDto datos);
    Task EliminarAsync(int id);
}

/// <summary>Anuncios del sitio público (HU-23): se publican solos dentro de su vigencia.</summary>
public class AnuncioService : IAnuncioService
{
    private const int MaximoVisibles = 2;

    private readonly AppDbContext _db;

    public AnuncioService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<AnuncioDto>> ListarVigentesAsync()
    {
        var ahora = DateTime.UtcNow;
        var lista = await _db.Anuncios.AsNoTracking()
            .Where(a => a.Activo && a.Desde <= ahora && (a.Hasta == null || a.Hasta > ahora))
            .OrderByDescending(a => a.Tipo == TiposAnuncio.Importante).ThenByDescending(a => a.Desde)
            .Take(MaximoVisibles)
            .ToListAsync();
        return lista.Select(ADto).ToList();
    }

    public async Task<List<AnuncioDto>> ListarAsync() =>
        (await _db.Anuncios.AsNoTracking().OrderByDescending(a => a.FechaCreacion).ToListAsync()).Select(ADto).ToList();

    public async Task<AnuncioDto> CrearAsync(AnuncioGuardarDto datos)
    {
        var anuncio = new Anuncio();
        Aplicar(anuncio, datos);
        _db.Anuncios.Add(anuncio);
        await _db.SaveChangesAsync();
        return ADto(anuncio);
    }

    public async Task<AnuncioDto> ActualizarAsync(int id, AnuncioGuardarDto datos)
    {
        var anuncio = await ObtenerAsync(id);
        Aplicar(anuncio, datos);
        await _db.SaveChangesAsync();
        return ADto(anuncio);
    }

    public async Task EliminarAsync(int id)
    {
        _db.Anuncios.Remove(await ObtenerAsync(id));
        await _db.SaveChangesAsync();
    }

    private async Task<Anuncio> ObtenerAsync(int id) =>
        await _db.Anuncios.FindAsync(id) ?? throw ReglaNegocioException.NoEncontrada("Anuncio no encontrado");

    private static void Aplicar(Anuncio a, AnuncioGuardarDto d)
    {
        var desde = d.Desde is null ? DateTime.UtcNow : AUtc(d.Desde.Value);
        var hasta = d.Hasta is null ? (DateTime?)null : AUtc(d.Hasta.Value);
        if (hasta is not null && hasta <= desde)
            throw ReglaNegocioException.Invalida("La fecha de fin debe ser posterior a la de inicio");

        var enlace = string.IsNullOrWhiteSpace(d.Enlace) ? null : d.Enlace.Trim();
        if (enlace is not null && !enlace.StartsWith('/') && !enlace.StartsWith("https://") && !enlace.StartsWith("http://"))
            throw ReglaNegocioException.Invalida("El enlace debe ser una ruta del sitio (/donar) o una dirección completa (https://...)");

        a.Texto = d.Texto.Trim();
        a.Enlace = enlace;
        a.Tipo = d.Tipo;
        a.Desde = desde;
        a.Hasta = hasta;
        a.Activo = d.Activo;
    }

    private static DateTime AUtc(DateTime fecha) =>
        fecha.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(fecha, DateTimeKind.Utc) : fecha.ToUniversalTime();

    private static AnuncioDto ADto(Anuncio a)
    {
        var ahora = DateTime.UtcNow;
        var situacion = !a.Activo ? "Desactivado" : a.Desde > ahora ? "Programado" : a.Hasta is not null && a.Hasta <= ahora ? "Vencido" : "Publicado";
        return new AnuncioDto
        {
            Id = a.Id,
            Texto = a.Texto,
            Enlace = a.Enlace,
            Tipo = a.Tipo,
            Desde = Mapeos.Utc(a.Desde),
            Hasta = a.Hasta is null ? null : Mapeos.Utc(a.Hasta.Value),
            Activo = a.Activo,
            Vigente = situacion == "Publicado",
            Situacion = situacion,
        };
    }
}

public interface IAlertaService
{
    Task<AlertasResumenDto> ListarAsync(bool soloPendientes);
    Task MarcarRevisadaAsync(int id, int actorId);
    Task MarcarTodasRevisadasAsync(int actorId);
}

/// <summary>Alertas de actividad inusual (HU-23). Las genera el inicio de sesión; el equipo administrador las revisa.</summary>
public class AlertaService : IAlertaService
{
    private const int MaximoListadas = 50;

    private readonly AppDbContext _db;

    public AlertaService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<AlertasResumenDto> ListarAsync(bool soloPendientes)
    {
        var consulta = _db.AlertasSeguridad.AsNoTracking().Include(a => a.Usuario).AsQueryable();
        if (soloPendientes)
            consulta = consulta.Where(a => !a.Revisada);

        var alertas = await consulta.OrderByDescending(a => a.Fecha).Take(MaximoListadas).ToListAsync();
        return new AlertasResumenDto
        {
            Pendientes = await _db.AlertasSeguridad.CountAsync(a => !a.Revisada),
            Alertas = alertas.Select(a => new AlertaSeguridadDto
            {
                Id = a.Id,
                Tipo = a.Tipo,
                Titulo = a.Tipo switch
                {
                    TiposAlerta.CuentaBloqueada => "Cuenta bloqueada por intentos fallidos",
                    TiposAlerta.IntentosFallidos => "Intentos fallidos de inicio de sesión",
                    _ => a.Tipo,
                },
                Detalle = a.Detalle,
                Cuenta = a.Usuario is null ? null : a.Usuario.Correo ?? a.Usuario.NombreUsuario,
                Fecha = Mapeos.Utc(a.Fecha),
                Revisada = a.Revisada,
            }).ToList(),
        };
    }

    public async Task MarcarRevisadaAsync(int id, int actorId)
    {
        var filas = await _db.AlertasSeguridad.Where(a => a.Id == id && !a.Revisada).ExecuteUpdateAsync(s => s
            .SetProperty(a => a.Revisada, true)
            .SetProperty(a => a.RevisadaPorUsuarioId, actorId)
            .SetProperty(a => a.FechaRevision, DateTime.UtcNow));
        if (filas == 0 && !await _db.AlertasSeguridad.AnyAsync(a => a.Id == id))
            throw ReglaNegocioException.NoEncontrada("Alerta no encontrada");
    }

    public Task MarcarTodasRevisadasAsync(int actorId) =>
        _db.AlertasSeguridad.Where(a => !a.Revisada).ExecuteUpdateAsync(s => s
            .SetProperty(a => a.Revisada, true)
            .SetProperty(a => a.RevisadaPorUsuarioId, actorId)
            .SetProperty(a => a.FechaRevision, DateTime.UtcNow));
}
