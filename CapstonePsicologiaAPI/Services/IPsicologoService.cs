using CapstonePsicologiaAPI.DTOs;

namespace CapstonePsicologiaAPI.Services;

public interface IPsicologoService
{
    Task<List<PsicologoListaDto>> ListarAsync(string? buscar, bool? activo);

    Task<PsicologoDetalleDto> ObtenerDetalleAsync(int id);

    /// <summary>Crea la cuenta de un psicólogo. Lanza conflicto si el correo ya está registrado.</summary>
    Task<PsicologoListaDto> CrearAsync(PsicologoCrearDto datos);

    Task<PsicologoListaDto> ActualizarAsync(int id, PsicologoActualizarDto datos);
}
