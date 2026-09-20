using CapstonePsicologiaAPI.DTOs;

namespace CapstonePsicologiaAPI.Services;

public interface ISolicitudService
{
    /// <summary>Crea la solicitud del usuario; devuelve null si ya tiene una abierta.</summary>
    Task<SolicitudDto?> CrearAsync(int usuarioId, SolicitudCrearDto datos);

    /// <summary>La solicitud más reciente del usuario, o null si nunca pidió orientación.</summary>
    Task<SolicitudDto?> ObtenerMiaAsync(int usuarioId);

    Task<List<SolicitudDto>> ListarAsync();

    /// <summary>Solicitudes abiertas asignadas a un psicólogo.</summary>
    Task<List<SolicitudDto>> ListarAsignadasAsync(int psicologoId);

    /// <summary>Cambia el estado y avisa por correo al usuario. Devuelve null si la solicitud no existe.</summary>
    Task<SolicitudDto?> CambiarEstadoAsync(int id, SolicitudEstadoDto datos);
}
