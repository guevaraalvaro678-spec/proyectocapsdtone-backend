using CapstonePsicologiaAPI.Models;

namespace CapstonePsicologiaAPI.Data;

public interface ICorreoData
{
    Task CrearVariosAsync(IEnumerable<CorreoSaliente> correos);

    Task<List<CorreoSaliente>> ObtenerPendientesAsync(int maximo);

    /// <summary>
    /// Toma un correo pendiente para enviarlo (Pendiente a Enviando). Devuelve false si otro proceso
    /// ya lo tomó, así dos servidores conectados a la misma base no lo envían dos veces.
    /// </summary>
    Task<bool> ReclamarAsync(int id);

    Task MarcarEnviadoAsync(int id);
    Task MarcarFallidoAsync(int id, string error);

    /// <summary>Un correo fallido vuelve a la cola. Devuelve false si no existe o no estaba fallido.</summary>
    Task<bool> ReintentarAsync(int id);

    Task<List<CorreoSaliente>> ObtenerHistorialAsync(int maximo);
    Task<int> ContarPendientesAsync();
}
