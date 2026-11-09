using CapstonePsicologiaAPI.Data;
using CapstonePsicologiaAPI.DTOs;
using CapstonePsicologiaAPI.Models;

namespace CapstonePsicologiaAPI.Services;

public interface IDonacionService
{
    Task<DonacionDto> RegistrarAsync(DonacionCrearDto datos);
    Task<List<DonacionDto>> ListarAsync(string? estado, string? buscar);
    Task<DonacionDto> CambiarEstadoAsync(int id, DonacionEstadoDto datos, int actorId);
    Task<DonacionResumenDto> ResumenAsync();
    Task<DonacionDto> GuardarObservacionesAsync(int id, string? observaciones, int actorId);
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
        _donacionData.AgregarHistorial(new HistorialDonacion
        {
            DonacionId = donacion.Id,
            Accion = "Registrada",
            Detalle = $"Desde el sitio web: {donacion.Articulos.Sum(a => a.Cantidad)} artículo(s)",
        });
        await _donacionData.GuardarAsync();
        return await ADtoAsync(donacion);
    }

    public async Task<List<DonacionDto>> ListarAsync(string? estado, string? buscar)
    {
        var lista = await _donacionData.ListarAsync(estado, buscar);
        var historial = (await _donacionData.ListarHistorialAsync(lista.Select(d => d.Id).ToArray())).ToLookup(h => h.DonacionId);
        return lista.Select(d => ADto(d, historial[d.Id])).ToList();
    }

    public async Task<DonacionDto> CambiarEstadoAsync(int id, DonacionEstadoDto datos, int actorId)
    {
        var donacion = await _donacionData.ObtenerPorIdAsync(id)
                       ?? throw ReglaNegocioException.NoEncontrada("Donación no encontrada");

        if (!EstadosDonacion.Acciones(donacion.Estado).Contains(datos.Estado))
            throw ReglaNegocioException.Invalida(
                $"No se puede pasar de \"{EstadosDonacion.Etiqueta(donacion.Estado)}\" a \"{EstadosDonacion.Etiqueta(datos.Estado)}\"");

        var destino = string.IsNullOrWhiteSpace(datos.DestinoEntrega) ? null : datos.DestinoEntrega.Trim();
        if (datos.Estado == EstadosDonacion.Entregada && destino is null)
            throw ReglaNegocioException.Invalida("Indica a quién o a qué actividad se entregó la donación");

        var observaciones = string.IsNullOrWhiteSpace(datos.Observaciones) ? null : datos.Observaciones.Trim();
        donacion.Estado = datos.Estado;
        donacion.Observaciones = observaciones ?? donacion.Observaciones;
        if (datos.Estado == EstadosDonacion.Entregada)
        {
            donacion.DestinoEntrega = destino;
            donacion.FechaEntrega = DateTime.UtcNow;
        }
        else
        {
            donacion.FechaRevision = DateTime.UtcNow;
        }

        _donacionData.AgregarHistorial(new HistorialDonacion
        {
            DonacionId = donacion.Id,
            Accion = EstadosDonacion.Etiqueta(datos.Estado),
            Detalle = datos.Estado == EstadosDonacion.Entregada ? $"Entregada a: {destino}" : observaciones,
            UsuarioId = actorId,
        });
        await _donacionData.GuardarAsync();
        return await ADtoAsync(donacion);
    }

    /// <summary>Cambia solo las observaciones (también permite borrarlas).</summary>
    public async Task<DonacionDto> GuardarObservacionesAsync(int id, string? observaciones, int actorId)
    {
        var donacion = await _donacionData.ObtenerPorIdAsync(id)
                       ?? throw ReglaNegocioException.NoEncontrada("Donación no encontrada");

        donacion.Observaciones = string.IsNullOrWhiteSpace(observaciones) ? null : observaciones.Trim();
        _donacionData.AgregarHistorial(new HistorialDonacion
        {
            DonacionId = donacion.Id,
            Accion = "Observaciones",
            Detalle = donacion.Observaciones ?? "Se borraron las observaciones",
            UsuarioId = actorId,
        });
        await _donacionData.GuardarAsync();
        return await ADtoAsync(donacion);
    }

    public async Task<DonacionResumenDto> ResumenAsync()
    {
        var porEstado = await _donacionData.ContarPorEstadoAsync();
        var inventario = await _donacionData.InventarioAsync();

        return new DonacionResumenDto
        {
            Pendientes = porEstado.GetValueOrDefault(EstadosDonacion.PendienteRecepcion),
            Recibidas = porEstado.GetValueOrDefault(EstadosDonacion.Recibida),
            Desestimadas = porEstado.GetValueOrDefault(EstadosDonacion.Desestimada),
            Entregadas = porEstado.GetValueOrDefault(EstadosDonacion.Entregada),
            Total = porEstado.Values.Sum(),
            Inventario = inventario.Select(a => new DonacionArticuloDto { Categoria = a.Categoria, Nombre = a.Nombre, Cantidad = a.Cantidad }).ToList(),
        };
    }

    private async Task<DonacionDto> ADtoAsync(Donacion d) => ADto(d, await _donacionData.ListarHistorialAsync(new[] { d.Id }));

    private static DonacionDto ADto(Donacion d, IEnumerable<HistorialDonacion> historial) => new()
    {
        Id = d.Id,
        Codigo = d.Codigo,
        DonanteNombre = d.DonanteNombre,
        Telefono = d.Telefono,
        Estado = d.Estado,
        Observaciones = d.Observaciones,
        FechaRegistro = Mapeos.Utc(d.FechaRegistro),
        FechaRevision = d.FechaRevision is null ? null : Mapeos.Utc(d.FechaRevision.Value),
        DestinoEntrega = d.DestinoEntrega,
        FechaEntrega = d.FechaEntrega is null ? null : Mapeos.Utc(d.FechaEntrega.Value),
        Articulos = d.Articulos.Select(a => new DonacionArticuloDto { Categoria = a.Categoria, Nombre = a.Nombre, Cantidad = a.Cantidad }).ToList(),
        Acciones = EstadosDonacion.Acciones(d.Estado),
        Historial = historial.Select(h => new HistorialDonacionDto
        {
            Accion = h.Accion,
            Detalle = h.Detalle,
            Responsable = h.Usuario is null ? null : string.IsNullOrWhiteSpace(h.Usuario.Nombre) ? h.Usuario.NombreUsuario : h.Usuario.Nombre,
            Fecha = Mapeos.Utc(h.Fecha),
        }).ToList(),
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
