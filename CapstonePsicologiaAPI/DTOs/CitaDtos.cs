using System.ComponentModel.DataAnnotations;
using CapstonePsicologiaAPI.Models;

namespace CapstonePsicologiaAPI.DTOs;

/// <summary>Cita tal como la ve quien la consulta. Las notas del psicólogo van vacías para el paciente.</summary>
public class CitaDto
{
    public int Id { get; set; }
    public int PacienteId { get; set; }
    public string PacienteNombre { get; set; } = string.Empty;
    public string? PacienteCorreo { get; set; }
    public int PsicologoId { get; set; }
    public string PsicologoNombre { get; set; } = string.Empty;
    public DateTime FechaInicio { get; set; }
    public int DuracionMinutos { get; set; }
    public string Modalidad { get; set; } = string.Empty;
    public string? Lugar { get; set; }
    public string Motivo { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public string? Notas { get; set; }
    public DateTime FechaCreacion { get; set; }
}

/// <summary>Datos con los que un administrador agenda o reprograma una cita.</summary>
public class CitaCrearDto
{
    [Required, Range(1, int.MaxValue)]
    public int PacienteId { get; set; }

    [Required, Range(1, int.MaxValue)]
    public int PsicologoId { get; set; }

    [Required]
    public DateTime FechaInicio { get; set; }

    [Range(15, 180)]
    public int DuracionMinutos { get; set; } = 50;

    [RegularExpression($"^({ModalidadesEvento.Presencial}|{ModalidadesEvento.Virtual})$", ErrorMessage = "Modalidad no válida")]
    public string Modalidad { get; set; } = ModalidadesEvento.Presencial;

    [StringLength(200)]
    public string? Lugar { get; set; }

    [Required, StringLength(300, MinimumLength = 3)]
    public string Motivo { get; set; } = string.Empty;
}

/// <summary>Cambio de estado de una cita, con notas opcionales del psicólogo.</summary>
public class CitaEstadoDto
{
    [Required]
    [RegularExpression(
        $"^({EstadosCita.Programada}|{EstadosCita.Confirmada}|{EstadosCita.Realizada}|{EstadosCita.Cancelada})$",
        ErrorMessage = "Estado no válido")]
    public string Estado { get; set; } = string.Empty;

    [StringLength(2000)]
    public string? Notas { get; set; }
}
