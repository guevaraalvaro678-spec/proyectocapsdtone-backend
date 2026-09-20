using CapstonePsicologiaAPI.Models;

namespace CapstonePsicologiaAPI.Data;

public interface ISolicitudData
{
    Task CrearAsync(SolicitudOrientacion solicitud);

    /// <summary>True si el usuario ya tiene una solicitud que no está cerrada.</summary>
    Task<bool> TieneAbiertaAsync(int usuarioId);

    Task<SolicitudOrientacion?> ObtenerPorIdAsync(int id);

    Task<SolicitudOrientacion?> ObtenerUltimaDeUsuarioAsync(int usuarioId);

    /// <summary>Todas las solicitudes de un usuario, la más reciente primero.</summary>
    Task<List<SolicitudOrientacion>> ObtenerDeUsuarioAsync(int usuarioId);

    Task<List<SolicitudOrientacion>> ObtenerTodasAsync();

    /// <summary>Solicitudes asignadas a un psicólogo que siguen abiertas.</summary>
    Task<List<SolicitudOrientacion>> ObtenerAsignadasAbiertasAsync(int psicologoId);

    /// <summary>Cuántas solicitudes abiertas tiene asignadas cada psicólogo.</summary>
    Task<Dictionary<int, int>> ContarAsignadasAbiertasAsync(int[] psicologoIds);

    /// <summary>
    /// Cambia el estado de una solicitud. Si se indica psicólogo, queda asignada a él.
    /// Devuelve null si la solicitud no existe.
    /// </summary>
    Task<SolicitudOrientacion?> CambiarEstadoAsync(int id, string estado, int? psicologoId);
}
