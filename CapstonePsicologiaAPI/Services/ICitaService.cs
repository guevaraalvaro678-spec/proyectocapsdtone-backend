using CapstonePsicologiaAPI.Data;
using CapstonePsicologiaAPI.DTOs;

namespace CapstonePsicologiaAPI.Services;

public interface ICitaService
{
    /// <summary>Citas que cumplen el filtro (vista del administrador, con notas).</summary>
    Task<List<CitaDto>> ListarAsync(CitaFiltro filtro);

    /// <summary>Citas de quien consulta: como paciente (usuario común) o como psicólogo.</summary>
    Task<List<CitaDto>> MiasAsync(int usuarioId, string rol);

    Task<CitaDto> CrearAsync(CitaCrearDto datos);

    Task<CitaDto> ActualizarAsync(int id, CitaCrearDto datos);

    /// <summary>Cambia el estado aplicando lo que cada rol puede hacer con la cita.</summary>
    Task<CitaDto> CambiarEstadoAsync(int id, CitaEstadoDto datos, int actorId, string actorRol);

    /// <summary>Envía el recordatorio de las citas de las próximas 24 horas.</summary>
    Task ProcesarRecordatoriosAsync();
}
