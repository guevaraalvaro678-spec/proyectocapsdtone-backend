using CapstonePsicologiaAPI.Data;
using CapstonePsicologiaAPI.DTOs;
using CapstonePsicologiaAPI.Models;

namespace CapstonePsicologiaAPI.Services;

public interface ISitioService
{
    Task<ImagenSubidaDto> SubirImagenAsync(IFormFile archivo);
    Task<Imagen> ObtenerImagenAsync(int id);
    Task<SitioPublicoDto> ObtenerPublicoAsync();
    Task GuardarLogoAsync(int? imagenId);
    Task<List<IniciativaDto>> ListarIniciativasAsync();
    Task<IniciativaDto> CrearIniciativaAsync(IniciativaGuardarDto datos);
    Task<IniciativaDto> ActualizarIniciativaAsync(int id, IniciativaGuardarDto datos);
    Task EliminarIniciativaAsync(int id);
}

/// <summary>Contenido editable del sitio público: logo e iniciativas, con imágenes subidas por el administrador.</summary>
public class SitioService : ISitioService
{
    private const int MaximoBytes = 2 * 1024 * 1024;

    // SVG queda fuera a propósito: puede llevar scripts.
    private static readonly HashSet<string> TiposPermitidos = new() { "image/png", "image/jpeg", "image/webp", "image/gif" };

    private readonly ISitioData _sitioData;

    public SitioService(ISitioData sitioData)
    {
        _sitioData = sitioData;
    }

    public async Task<ImagenSubidaDto> SubirImagenAsync(IFormFile archivo)
    {
        if (archivo.Length == 0)
            throw ReglaNegocioException.Invalida("El archivo está vacío");

        if (archivo.Length > MaximoBytes)
            throw ReglaNegocioException.Invalida("La imagen pesa más de 2 MB");

        if (!TiposPermitidos.Contains(archivo.ContentType))
            throw ReglaNegocioException.Invalida("Solo se aceptan imágenes PNG, JPG, WEBP o GIF");

        using var memoria = new MemoryStream();
        await archivo.CopyToAsync(memoria);

        var imagen = await _sitioData.CrearImagenAsync(new Imagen
        {
            Nombre = Path.GetFileName(archivo.FileName),
            TipoContenido = archivo.ContentType,
            Datos = memoria.ToArray(),
        });

        return new ImagenSubidaDto { Id = imagen.Id, Url = Url(imagen.Id)! };
    }

    public async Task<Imagen> ObtenerImagenAsync(int id) =>
        await _sitioData.ObtenerImagenAsync(id) ?? throw ReglaNegocioException.NoEncontrada("Imagen no encontrada");

    public async Task<SitioPublicoDto> ObtenerPublicoAsync()
    {
        var config = await _sitioData.ObtenerConfigAsync();
        var iniciativas = await _sitioData.ListarIniciativasAsync(soloActivas: true);

        return new SitioPublicoDto
        {
            LogoUrl = Url(config.LogoImagenId),
            Iniciativas = iniciativas.Select(ADto).ToList(),
        };
    }

    public async Task GuardarLogoAsync(int? imagenId)
    {
        if (imagenId is not null && await _sitioData.ObtenerImagenAsync(imagenId.Value) is null)
            throw ReglaNegocioException.Invalida("La imagen no existe");

        var config = await _sitioData.ObtenerConfigAsync();
        var anterior = config.LogoImagenId;
        config.LogoImagenId = imagenId;
        await _sitioData.GuardarAsync();

        if (anterior != imagenId)
            await _sitioData.EliminarImagenAsync(anterior);
    }

    public async Task<List<IniciativaDto>> ListarIniciativasAsync() =>
        (await _sitioData.ListarIniciativasAsync(soloActivas: false)).Select(ADto).ToList();

    public async Task<IniciativaDto> CrearIniciativaAsync(IniciativaGuardarDto datos)
    {
        await ValidarImagenAsync(datos.ImagenId);

        var iniciativa = new Iniciativa();
        Aplicar(iniciativa, datos);
        await _sitioData.CrearIniciativaAsync(iniciativa);
        return ADto(iniciativa);
    }

    public async Task<IniciativaDto> ActualizarIniciativaAsync(int id, IniciativaGuardarDto datos)
    {
        var iniciativa = await _sitioData.ObtenerIniciativaAsync(id)
                         ?? throw ReglaNegocioException.NoEncontrada("Iniciativa no encontrada");

        await ValidarImagenAsync(datos.ImagenId);

        var anterior = iniciativa.ImagenId;
        Aplicar(iniciativa, datos);
        await _sitioData.GuardarAsync();

        if (anterior != iniciativa.ImagenId)
            await _sitioData.EliminarImagenAsync(anterior);

        return ADto(iniciativa);
    }

    public async Task EliminarIniciativaAsync(int id)
    {
        var iniciativa = await _sitioData.ObtenerIniciativaAsync(id)
                         ?? throw ReglaNegocioException.NoEncontrada("Iniciativa no encontrada");

        var imagenId = iniciativa.ImagenId;
        await _sitioData.EliminarIniciativaAsync(iniciativa);
        await _sitioData.EliminarImagenAsync(imagenId);
    }

    private async Task ValidarImagenAsync(int? imagenId)
    {
        if (imagenId is not null && await _sitioData.ObtenerImagenAsync(imagenId.Value) is null)
            throw ReglaNegocioException.Invalida("La imagen no existe");
    }

    private static void Aplicar(Iniciativa i, IniciativaGuardarDto d)
    {
        i.Titulo = d.Titulo.Trim();
        i.Texto = d.Texto.Trim();
        i.Icono = string.IsNullOrWhiteSpace(d.Icono) ? "⭐" : d.Icono.Trim();
        i.Color = d.Color;
        i.ImagenId = d.ImagenId;
        i.Orden = d.Orden;
        i.Activa = d.Activa;
    }

    private static string? Url(int? imagenId) => imagenId is null ? null : $"/api/imagen/{imagenId}";

    private static IniciativaDto ADto(Iniciativa i) => new()
    {
        Id = i.Id,
        Titulo = i.Titulo,
        Texto = i.Texto,
        Icono = i.Icono,
        Color = i.Color,
        ImagenId = i.ImagenId,
        ImagenUrl = Url(i.ImagenId),
        Orden = i.Orden,
        Activa = i.Activa,
    };
}
