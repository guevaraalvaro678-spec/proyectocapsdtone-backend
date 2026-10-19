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
    Task GuardarYapeAsync(YapeGuardarDto datos);
    Task<List<IniciativaDto>> ListarIniciativasAsync();
    Task<IniciativaDto> CrearIniciativaAsync(IniciativaGuardarDto datos);
    Task<IniciativaDto> ActualizarIniciativaAsync(int id, IniciativaGuardarDto datos);
    Task EliminarIniciativaAsync(int id);

    /// <summary>Iniciativas que propuso un voluntario o practicante.</summary>
    Task<List<IniciativaDto>> ListarMiasAsync(int usuarioId);
    Task<IniciativaDto> ProponerAsync(int usuarioId, IniciativaPropuestaDto datos);
    Task<IniciativaDto> RevisarAsync(int id, IniciativaRevisionDto datos);
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
            Yape = config.YapeNumero is null && config.YapeQrImagenId is null
                ? null
                : new YapeDto
                {
                    Numero = config.YapeNumero,
                    NumeroVisible = config.YapeNumero is { Length: 9 } n ? $"{n[..3]} {n[3..6]} {n[6..]}" : config.YapeNumero,
                    Titular = config.YapeTitular,
                    QrImagenId = config.YapeQrImagenId,
                    QrUrl = Url(config.YapeQrImagenId),
                },
        };
    }

    /// <summary>Guarda el número, el titular y el QR de Yape. Si cambia el QR, borra la imagen anterior.</summary>
    public async Task GuardarYapeAsync(YapeGuardarDto datos)
    {
        if (datos.QrImagenId is not null && await _sitioData.ObtenerImagenAsync(datos.QrImagenId.Value) is null)
            throw ReglaNegocioException.Invalida("La imagen del QR no existe");

        var numero = string.IsNullOrWhiteSpace(datos.Numero) ? null : datos.Numero.Trim();
        if (numero is null && datos.QrImagenId is null && !string.IsNullOrWhiteSpace(datos.Titular))
            throw ReglaNegocioException.Invalida("Agrega el número de Yape o la imagen del QR");

        var config = await _sitioData.ObtenerConfigAsync();
        var qrAnterior = config.YapeQrImagenId;
        config.YapeNumero = numero;
        config.YapeTitular = string.IsNullOrWhiteSpace(datos.Titular) || (numero is null && datos.QrImagenId is null) ? null : datos.Titular.Trim();
        config.YapeQrImagenId = datos.QrImagenId;
        await _sitioData.GuardarAsync();

        if (qrAnterior != datos.QrImagenId)
            await _sitioData.EliminarImagenAsync(qrAnterior);
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

    public async Task<List<IniciativaDto>> ListarMiasAsync(int usuarioId) =>
        (await _sitioData.ListarIniciativasDeUsuarioAsync(usuarioId)).Select(ADto).ToList();

    public async Task<IniciativaDto> ProponerAsync(int usuarioId, IniciativaPropuestaDto datos)
    {
        var iniciativa = new Iniciativa
        {
            Titulo = datos.Titulo.Trim(),
            Texto = datos.Texto.Trim(),
            Icono = "💡",
            Color = ColoresIniciativa.Verde,
            CreadaPorUsuarioId = usuarioId,
            Estado = EstadosIniciativa.Pendiente,
            Orden = 100,
        };

        await _sitioData.CrearIniciativaAsync(iniciativa);
        return ADto(iniciativa);
    }

    /// <summary>Aprobada: aparece en el sitio público. Rechazada: queda visible solo para quien la propuso, con el motivo.</summary>
    public async Task<IniciativaDto> RevisarAsync(int id, IniciativaRevisionDto datos)
    {
        var iniciativa = await _sitioData.ObtenerIniciativaAsync(id)
                         ?? throw ReglaNegocioException.NoEncontrada("Iniciativa no encontrada");

        if (iniciativa.Estado != EstadosIniciativa.Pendiente)
            throw ReglaNegocioException.Conflicto("Esta iniciativa ya fue revisada");

        iniciativa.Estado = datos.Estado;
        iniciativa.MotivoRechazo = datos.Estado == EstadosIniciativa.Rechazada && !string.IsNullOrWhiteSpace(datos.Motivo) ? datos.Motivo.Trim() : null;
        await _sitioData.GuardarAsync();
        return ADto(iniciativa);
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
        Estado = i.Estado,
        MotivoRechazo = i.MotivoRechazo,
        CreadaPorNombre = i.CreadaPor?.Nombre,
        CreadaPorRol = i.CreadaPor?.Rol,
        FechaCreacion = Mapeos.Utc(i.FechaCreacion),
    };
}
