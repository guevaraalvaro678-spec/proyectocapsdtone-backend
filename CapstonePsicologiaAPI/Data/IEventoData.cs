using CapstonePsicologiaAPI.Models;

namespace CapstonePsicologiaAPI.Data;

public interface IEventoData
{
    /// <summary>Eventos publicados que empiezan desde `desde`, del más próximo al más lejano.</summary>
    Task<List<Evento>> ObtenerPublicosAsync(DateTime desde, int maximo);

    /// <summary>Todos los eventos (publicados o no, pasados o futuros), el más reciente primero.</summary>
    Task<List<Evento>> ObtenerTodosAsync();

    Task<Evento?> ObtenerPorIdAsync(int id);
    Task CrearAsync(Evento evento);
    Task ActualizarAsync(Evento evento);
    Task EliminarAsync(Evento evento);
}
