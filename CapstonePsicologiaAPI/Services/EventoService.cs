using CapstonePsicologiaAPI.Data;
using CapstonePsicologiaAPI.DTOs;
using CapstonePsicologiaAPI.Models;

namespace CapstonePsicologiaAPI.Services;

public class EventoService : IEventoService
{
    private const int MaximoEnBanner = 12;

    private readonly IEventoData _eventoData;

    public EventoService(IEventoData eventoData)
    {
        _eventoData = eventoData;
    }

    public async Task<List<EventoDto>> ListarPublicosAsync()
    {
        // Un evento de hoy sigue apareciendo aunque su hora ya haya pasado.
        var eventos = await _eventoData.ObtenerPublicosAsync(DateTime.UtcNow.Date, MaximoEnBanner);
        return eventos.Select(ADto).ToList();
    }

    public async Task<List<EventoDto>> ListarTodosAsync()
    {
        var eventos = await _eventoData.ObtenerTodosAsync();
        return eventos.Select(ADto).ToList();
    }

    public async Task<EventoDto> CrearAsync(EventoGuardarDto datos)
    {
        var evento = new Evento { FechaCreacion = DateTime.UtcNow };
        Aplicar(evento, datos);

        await _eventoData.CrearAsync(evento);
        return ADto(evento);
    }

    public async Task<EventoDto?> ActualizarAsync(int id, EventoGuardarDto datos)
    {
        var evento = await _eventoData.ObtenerPorIdAsync(id);

        if (evento is null)
            return null;

        Aplicar(evento, datos);
        await _eventoData.ActualizarAsync(evento);
        return ADto(evento);
    }

    public async Task<bool> EliminarAsync(int id)
    {
        var evento = await _eventoData.ObtenerPorIdAsync(id);

        if (evento is null)
            return false;

        await _eventoData.EliminarAsync(evento);
        return true;
    }

    private static void Aplicar(Evento evento, EventoGuardarDto datos)
    {
        evento.Titulo = datos.Titulo.Trim();
        evento.Descripcion = datos.Descripcion.Trim();
        evento.FechaInicio = AUtc(datos.FechaInicio);
        evento.Lugar = datos.Lugar.Trim();
        evento.Modalidad = datos.Modalidad;
        evento.ImagenUrl = string.IsNullOrWhiteSpace(datos.ImagenUrl) ? null : datos.ImagenUrl.Trim();
        evento.Publicado = datos.Publicado;
        evento.Tipo = datos.Tipo;
    }

    /// <summary>El front envía la hora en UTC; si llegara sin zona se toma como UTC, sin conversiones.</summary>
    private static DateTime AUtc(DateTime fecha) =>
        fecha.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(fecha, DateTimeKind.Utc) : fecha.ToUniversalTime();

    private static EventoDto ADto(Evento e) => new()
    {
        Id = e.Id,
        Titulo = e.Titulo,
        Descripcion = e.Descripcion,
        // SQL Server devuelve la fecha sin zona; se marca como UTC para que el front la convierta bien.
        FechaInicio = DateTime.SpecifyKind(e.FechaInicio, DateTimeKind.Utc),
        Lugar = e.Lugar,
        Modalidad = e.Modalidad,
        ImagenUrl = e.ImagenUrl,
        Tipo = e.Tipo,
        Publicado = e.Publicado,
    };
}
