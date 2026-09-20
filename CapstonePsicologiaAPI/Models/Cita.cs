namespace CapstonePsicologiaAPI.Models;

/// <summary>Cita entre un paciente (usuario común) y un psicólogo.</summary>
public class Cita
{
    public int Id { get; set; }

    public int PacienteId { get; set; }
    public Usuario? Paciente { get; set; }

    public int PsicologoId { get; set; }
    public Usuario? Psicologo { get; set; }

    /// <summary>Inicio de la cita en UTC.</summary>
    public DateTime FechaInicio { get; set; }

    public int DuracionMinutos { get; set; } = 50;
    public string Modalidad { get; set; } = ModalidadesEvento.Presencial;

    /// <summary>Consultorio o enlace de la videollamada.</summary>
    public string? Lugar { get; set; }

    public string Motivo { get; set; } = string.Empty;
    public string Estado { get; set; } = EstadosCita.Programada;

    /// <summary>Notas del psicólogo. No las ve el paciente.</summary>
    public string? Notas { get; set; }

    public bool RecordatorioEnviado { get; set; }
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
}

public static class EstadosCita
{
    public const string Programada = "Programada";
    public const string Confirmada = "Confirmada";
    public const string Realizada = "Realizada";
    public const string Cancelada = "Cancelada";
}
