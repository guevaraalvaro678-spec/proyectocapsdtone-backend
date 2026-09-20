using System.ComponentModel.DataAnnotations;
using CapstonePsicologiaAPI.Models;

namespace CapstonePsicologiaAPI.DTOs;

/// <summary>Bloque semanal de horario de un psicólogo (hora de Lima).</summary>
public class DisponibilidadDto
{
    public int Id { get; set; }
    public int PsicologoId { get; set; }

    /// <summary>0 = domingo ... 6 = sábado.</summary>
    public int DiaSemana { get; set; }

    /// <summary>Formato HH:mm.</summary>
    public string HoraInicio { get; set; } = string.Empty;

    public string HoraFin { get; set; } = string.Empty;
    public int DuracionMinutos { get; set; }
    public string Modalidad { get; set; } = string.Empty;
    public string? Lugar { get; set; }
    public bool Activo { get; set; }
}

public class DisponibilidadGuardarDto
{
    [Range(0, 6)]
    public int DiaSemana { get; set; }

    [Required, RegularExpression(@"^([01]\d|2[0-3]):[0-5]\d$", ErrorMessage = "Hora inválida (HH:mm)")]
    public string HoraInicio { get; set; } = string.Empty;

    [Required, RegularExpression(@"^([01]\d|2[0-3]):[0-5]\d$", ErrorMessage = "Hora inválida (HH:mm)")]
    public string HoraFin { get; set; } = string.Empty;

    [Range(20, 120)]
    public int DuracionMinutos { get; set; } = 60;

    [RegularExpression(
        $"^({ModalidadesEvento.Presencial}|{ModalidadesEvento.Virtual}|{ModalidadesEvento.Mixta})$",
        ErrorMessage = "Modalidad no válida")]
    public string Modalidad { get; set; } = ModalidadesEvento.Mixta;

    [StringLength(200)]
    public string? Lugar { get; set; }

    public bool Activo { get; set; } = true;
}

/// <summary>Un horario concreto en el que un paciente puede reservar cita.</summary>
public class HorarioDisponibleDto
{
    /// <summary>Inicio en UTC.</summary>
    public DateTime Inicio { get; set; }

    public int DuracionMinutos { get; set; }

    /// <summary>Presencial, Virtual o Mixta (el paciente elige entre las dos).</summary>
    public string Modalidad { get; set; } = string.Empty;

    public string? Lugar { get; set; }
}

/// <summary>Lo que ve un paciente de un psicólogo al elegirlo: solo nombre y especialidad.</summary>
public class PsicologoPublicoDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Especialidad { get; set; }
}

/// <summary>Reserva que hace un paciente de un horario disponible.</summary>
public class ReservaCrearDto
{
    [Range(1, int.MaxValue)]
    public int PsicologoId { get; set; }

    [Required]
    public DateTime FechaInicio { get; set; }

    [RegularExpression($"^({ModalidadesEvento.Presencial}|{ModalidadesEvento.Virtual})$", ErrorMessage = "Modalidad no válida")]
    public string Modalidad { get; set; } = ModalidadesEvento.Presencial;

    [Required, StringLength(300, MinimumLength = 3)]
    public string Motivo { get; set; } = string.Empty;
}
