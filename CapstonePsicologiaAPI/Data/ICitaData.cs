using CapstonePsicologiaAPI.Models;

namespace CapstonePsicologiaAPI.Data;

public interface ICitaData
{
    Task CrearAsync(Cita cita);

    /// <summary>La cita con su paciente y psicólogo cargados (para modificarla).</summary>
    Task<Cita?> ObtenerPorIdAsync(int id);

    Task ActualizarAsync(Cita cita);

    /// <summary>Citas que cumplen el filtro, de la más próxima a la más lejana, con paciente y psicólogo.</summary>
    Task<List<Cita>> ListarAsync(CitaFiltro filtro);

    /// <summary>
    /// True si el psicólogo (o el paciente) ya tiene una cita programada o confirmada que se cruza con el
    /// intervalo. `excluirId` sirve al reprogramar una cita para no compararla consigo misma.
    /// </summary>
    Task<bool> HaySolapamientoAsync(int? psicologoId, int? pacienteId, DateTime inicio, DateTime fin, int? excluirId);

    /// <summary>Citas programadas o confirmadas entre las dos fechas a las que aún no se envió recordatorio.</summary>
    Task<List<Cita>> ObtenerParaRecordatorioAsync(DateTime desde, DateTime hasta);

    /// <summary>Marca el recordatorio como enviado. Devuelve false si otro proceso ya lo hizo.</summary>
    Task<bool> ReclamarRecordatorioAsync(int id);

    /// <summary>Cuántas citas programadas o confirmadas futuras tiene un paciente.</summary>
    Task<int> ContarActivasFuturasDePacienteAsync(int pacienteId, DateTime desde);

    /// <summary>Cuántas citas próximas (programadas o confirmadas) tiene cada psicólogo.</summary>
    Task<Dictionary<int, int>> ContarProximasPorPsicologoAsync(int[] psicologoIds, DateTime desde);
}
