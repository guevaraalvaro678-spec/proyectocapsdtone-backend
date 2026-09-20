namespace CapstonePsicologiaAPI.Models;

/// <summary>
/// Bloque de horario que un psicólogo deja disponible cada semana (en hora de Lima). El bloque se
/// divide en citas consecutivas de `DuracionMinutos`: por ejemplo lunes de 09:00 a 13:00 con citas de
/// 60 minutos ofrece 09:00, 10:00, 11:00 y 12:00.
/// </summary>
public class Disponibilidad
{
    public int Id { get; set; }

    public int PsicologoId { get; set; }
    public Usuario? Psicologo { get; set; }

    /// <summary>Día de la semana: 0 = domingo ... 6 = sábado (como DayOfWeek).</summary>
    public int DiaSemana { get; set; }

    public TimeSpan HoraInicio { get; set; }
    public TimeSpan HoraFin { get; set; }

    public int DuracionMinutos { get; set; } = 60;

    /// <summary>Presencial, Virtual o Mixta (el paciente elige).</summary>
    public string Modalidad { get; set; } = ModalidadesEvento.Mixta;

    /// <summary>Consultorio o enlace de la videollamada que se copia a las citas reservadas.</summary>
    public string? Lugar { get; set; }

    public bool Activo { get; set; } = true;
}
