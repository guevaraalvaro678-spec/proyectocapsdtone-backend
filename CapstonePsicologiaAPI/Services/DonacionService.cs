using CapstonePsicologiaAPI.Data;
using CapstonePsicologiaAPI.DTOs;
using CapstonePsicologiaAPI.Models;

namespace CapstonePsicologiaAPI.Services;

public interface IDonacionService
{
    Task<DonacionDto> RegistrarAsync(DonacionCrearDto datos);
    Task<List<DonacionDto>> ListarAsync(string? estado, string? buscar);
    Task<DonacionDto> CambiarEstadoAsync(int id, DonacionEstadoDto datos);
}

/// <summary>Donaciones de artículos físicos: se registran en el sitio público y el administrador confirma su recepción.</summary>
public class DonacionService : IDonacionService
{
    private readonly IDonacionData _donacionData;

    public DonacionService(IDonacionData donacionData)
    {
        _donacionData = donacionData;
    }

    public async Task<DonacionDto> RegistrarAsync(DonacionCrearDto datos)
    {
        var donacion = new Donacion
        {
            Codigo = "TMP-" + Guid.NewGuid().ToString("N")[..16],
            DonanteNombre = datos.DonanteNombre.Trim(),
            Telefono = datos.Telefono.Trim(),
            Articulos = datos.Articulos.Select(a => new DonacionArticulo
            {
                Categoria = a.Categoria,
                Nombre = a.Nombre.Trim(),
                Cantidad = a.Cantidad,
            }).ToList(),
        };

        await _donacionData.CrearAsync(donacion);
        donacion.Codigo = $"DON-{donacion.FechaRegistro.Year}-{donacion.Id:000}";
        await _donacionData.GuardarAsync();

        return ADto(donacion);
    }

    public async Task<List<DonacionDto>> ListarAsync(string? estado, string? buscar) =>
        (await _donacionData.ListarAsync(estado, buscar)).Select(ADto).ToList();

    public async Task<DonacionDto> CambiarEstadoAsync(int id, DonacionEstadoDto datos)
    {
        var donacion = await _donacionData.ObtenerPorIdAsync(id)
                       ?? throw ReglaNegocioException.NoEncontrada("Donación no encontrada");

        donacion.Estado = datos.Estado;
        donacion.Observaciones = string.IsNullOrWhiteSpace(datos.Observaciones) ? donacion.Observaciones : datos.Observaciones.Trim();
        donacion.FechaRevision = DateTime.UtcNow;
        await _donacionData.GuardarAsync();
        return ADto(donacion);
    }

    private static DonacionDto ADto(Donacion d) => new()
    {
        Id = d.Id,
        Codigo = d.Codigo,
        DonanteNombre = d.DonanteNombre,
        Telefono = d.Telefono,
        Estado = d.Estado,
        Observaciones = d.Observaciones,
        FechaRegistro = Mapeos.Utc(d.FechaRegistro),
        Articulos = d.Articulos.Select(a => new DonacionArticuloDto { Categoria = a.Categoria, Nombre = a.Nombre, Cantidad = a.Cantidad }).ToList(),
    };
}

public interface INoticiaService
{
    Task<List<NoticiaDto>> ListarPublicadasAsync(int maximo);
    Task<List<NoticiaDto>> ListarTodasAsync();
    Task<NoticiaDto> CrearAsync(NoticiaGuardarDto datos);
    Task<NoticiaDto> ActualizarAsync(int id, NoticiaGuardarDto datos);
    Task EliminarAsync(int id);
}

/// <summary>Noticias del sitio público. Solo las publicadas se muestran a los visitantes.</summary>
public class NoticiaService : INoticiaService
{
    private readonly INoticiaData _noticiaData;
    private readonly ISitioData _sitioData;

    public NoticiaService(INoticiaData noticiaData, ISitioData sitioData)
    {
        _noticiaData = noticiaData;
        _sitioData = sitioData;
    }

    public async Task<List<NoticiaDto>> ListarPublicadasAsync(int maximo) =>
        (await _noticiaData.ListarAsync(soloPublicadas: true, maximo)).Select(ADto).ToList();

    public async Task<List<NoticiaDto>> ListarTodasAsync() =>
        (await _noticiaData.ListarAsync(soloPublicadas: false, null)).Select(ADto).ToList();

    public async Task<NoticiaDto> CrearAsync(NoticiaGuardarDto datos)
    {
        await ValidarImagenAsync(datos.ImagenId);
        var noticia = new Noticia();
        Aplicar(noticia, datos);
        await _noticiaData.CrearAsync(noticia);
        return ADto(noticia);
    }

    public async Task<NoticiaDto> ActualizarAsync(int id, NoticiaGuardarDto datos)
    {
        var noticia = await _noticiaData.ObtenerPorIdAsync(id) ?? throw ReglaNegocioException.NoEncontrada("Noticia no encontrada");
        await ValidarImagenAsync(datos.ImagenId);

        var imagenAnterior = noticia.ImagenId;
        Aplicar(noticia, datos);
        await _noticiaData.GuardarAsync();

        if (imagenAnterior != noticia.ImagenId)
            await _sitioData.EliminarImagenAsync(imagenAnterior);

        return ADto(noticia);
    }

    public async Task EliminarAsync(int id)
    {
        var noticia = await _noticiaData.ObtenerPorIdAsync(id) ?? throw ReglaNegocioException.NoEncontrada("Noticia no encontrada");
        var imagenId = noticia.ImagenId;
        await _noticiaData.EliminarAsync(noticia);
        await _sitioData.EliminarImagenAsync(imagenId);
    }

    private async Task ValidarImagenAsync(int? imagenId)
    {
        if (imagenId is not null && await _sitioData.ObtenerImagenAsync(imagenId.Value) is null)
            throw ReglaNegocioException.Invalida("La imagen no existe");
    }

    private static void Aplicar(Noticia n, NoticiaGuardarDto d)
    {
        n.Titulo = d.Titulo.Trim();
        n.Resumen = d.Resumen.Trim();
        n.Contenido = string.IsNullOrWhiteSpace(d.Contenido) ? null : d.Contenido.Trim();
        n.ImagenId = d.ImagenId;
        n.Estado = d.Estado;
        if (d.Fecha is not null)
            n.Fecha = d.Fecha.Value.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(d.Fecha.Value, DateTimeKind.Utc) : d.Fecha.Value.ToUniversalTime();
    }

    private static NoticiaDto ADto(Noticia n) => new()
    {
        Id = n.Id,
        Titulo = n.Titulo,
        Resumen = n.Resumen,
        Contenido = n.Contenido,
        ImagenId = n.ImagenId,
        ImagenUrl = n.ImagenId is null ? null : $"/api/imagen/{n.ImagenId}",
        Estado = n.Estado,
        Fecha = Mapeos.Utc(n.Fecha),
    };
}
