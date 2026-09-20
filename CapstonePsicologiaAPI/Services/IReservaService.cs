using CapstonePsicologiaAPI.DTOs;

namespace CapstonePsicologiaAPI.Services;

/// <summary>Reserva de citas que hace el propio paciente eligiendo un horario que dejó libre el psicólogo.</summary>
public interface IReservaService
{
    /// <summary>Psicólogos activos que tienen horarios para reservar.</summary>
    Task<List<PsicologoPublicoDto>> ListarPsicologosAsync();

    Task<List<HorarioDisponibleDto>> ListarHorariosAsync(int psicologoId, int dias);

    Task<CitaDto> ReservarAsync(int pacienteId, ReservaCrearDto datos);
}
