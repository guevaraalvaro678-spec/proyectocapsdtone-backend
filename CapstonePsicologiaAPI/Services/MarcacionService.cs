using CapstonePsicologiaAPI.Data;
using CapstonePsicologiaAPI.DTOs;
using CapstonePsicologiaAPI.Models;

namespace CapstonePsicologiaAPI.Services;

public interface IMarcacionService
{
    Task<MarcacionDto> MarcarEntradaAsync(int usuarioId);
    Task<MarcacionDto> MarcarSalidaAsync(int usuarioId);
    Task<List<MarcacionDto>> MiasAsync(int usuarioId);
    Task<List<MarcacionDto>> ListarAsync(bool soloHoy, int? usuarioId);
}

/// <summary>Asistencia de los practicantes: cada uno marca su entrada y su salida y el administrador lo ve.</summary>
public class MarcacionService : IMarcacionService
{
    private const int MaximoMarcaciones = 200;

    private readonly IMarcacionData _marcacionData;
    private readonly IUsuarioData _usuarioData;

    public MarcacionService(IMarcacionData marcacionData, IUsuarioData usuarioData)
    {
        _marcacionData = marcacionData;
        _usuarioData = usuarioData;
    }

    public async Task<MarcacionDto> MarcarEntradaAsync(int usuarioId)
    {
        if (await _marcacionData.ObtenerAbiertaAsync(usuarioId) is not null)
            throw ReglaNegocioException.Conflicto("Ya marcaste tu entrada; marca primero tu salida");

        var usuario = await _usuarioData.ObtenerPorIdAsync(usuarioId)
                      ?? throw ReglaNegocioException.NoEncontrada("Usuario no encontrado");

        var marcacion = new Marcacion { UsuarioId = usuarioId, Entrada = DateTime.UtcNow };
        await _marcacionData.CrearAsync(marcacion);
        marcacion.Usuario = usuario;
        return ADto(marcacion);
    }

    public async Task<MarcacionDto> MarcarSalidaAsync(int usuarioId)
    {
        var marcacion = await _marcacionData.ObtenerAbiertaAsync(usuarioId)
                        ?? throw ReglaNegocioException.Conflicto("No tienes una entrada marcada");

        marcacion.Salida = DateTime.UtcNow;
        await _marcacionData.GuardarAsync();
        return ADto(marcacion);
    }

    public async Task<List<MarcacionDto>> MiasAsync(int usuarioId) =>
        (await _marcacionData.ListarAsync(usuarioId, null, 30)).Select(ADto).ToList();

    public async Task<List<MarcacionDto>> ListarAsync(bool soloHoy, int? usuarioId)
    {
        // "Hoy" según la hora de Lima (UTC-5).
        DateTime? desde = soloHoy ? DateTime.UtcNow.AddHours(-5).Date.AddHours(5) : null;
        return (await _marcacionData.ListarAsync(usuarioId, desde, MaximoMarcaciones)).Select(ADto).ToList();
    }

    private static MarcacionDto ADto(Marcacion m) => new()
    {
        Id = m.Id,
        UsuarioId = m.UsuarioId,
        UsuarioNombre = m.Usuario?.Nombre ?? string.Empty,
        Entrada = Mapeos.Utc(m.Entrada),
        Salida = m.Salida is null ? null : Mapeos.Utc(m.Salida.Value),
    };
}
