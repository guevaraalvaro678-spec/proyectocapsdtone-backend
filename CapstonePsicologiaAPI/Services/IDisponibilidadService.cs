using CapstonePsicologiaAPI.DTOs;

namespace CapstonePsicologiaAPI.Services;

public interface IDisponibilidadService
{
    /// <summary>Los bloques de horario de un psicólogo, activos o no.</summary>
    Task<List<DisponibilidadDto>> ListarDePsicologoAsync(int psicologoId);

    Task<DisponibilidadDto> CrearAsync(int psicologoId, DisponibilidadGuardarDto datos);

    /// <summary>Modifica un bloque. Solo su dueño puede hacerlo.</summary>
    Task<DisponibilidadDto> ActualizarAsync(int id, int psicologoId, DisponibilidadGuardarDto datos);

    /// <summary>Elimina un bloque. Solo su dueño puede hacerlo. Las citas ya reservadas no se tocan.</summary>
    Task EliminarAsync(int id, int psicologoId);

    /// <summary>Horarios libres de un psicólogo en los próximos `dias` días.</summary>
    Task<List<HorarioDisponibleDto>> ObtenerHorariosAsync(int psicologoId, int dias);

    /// <summary>El horario libre que empieza exactamente en `inicioUtc`, o null si no existe o ya no está libre.</summary>
    Task<HorarioDisponibleDto?> BuscarHorarioAsync(int psicologoId, DateTime inicioUtc);
}
