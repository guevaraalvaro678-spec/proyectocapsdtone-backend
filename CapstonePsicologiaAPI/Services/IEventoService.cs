using CapstonePsicologiaAPI.DTOs;

namespace CapstonePsicologiaAPI.Services;

public interface IEventoService
{
    /// <summary>Los próximos eventos publicados, para el banner del sitio público.</summary>
    Task<List<EventoDto>> ListarPublicosAsync();

    /// <summary>Todos los eventos, para el panel de administración.</summary>
    Task<List<EventoDto>> ListarTodosAsync();

    Task<EventoDto> CrearAsync(EventoGuardarDto datos);

    /// <summary>Devuelve null si el evento no existe.</summary>
    Task<EventoDto?> ActualizarAsync(int id, EventoGuardarDto datos);

    /// <summary>Devuelve false si el evento no existe.</summary>
    Task<bool> EliminarAsync(int id);
}
