using System.ComponentModel.DataAnnotations;

namespace CapstonePsicologiaAPI.DTOs;

/// <summary>Registro público de un practicante: todos los campos son obligatorios.</summary>
public class PracticanteRegistroDto
{
    [Required, RegularExpression(@"^\d{8}$", ErrorMessage = "El DNI debe tener 8 dígitos")]
    public string Dni { get; set; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 2)]
    public string Nombres { get; set; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 2)]
    public string Apellidos { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(150)]
    public string Correo { get; set; } = string.Empty;

    [Required, StringLength(30, MinimumLength = 6)]
    public string Telefono { get; set; } = string.Empty;

    [Required, StringLength(150, MinimumLength = 2)]
    public string Universidad { get; set; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 2)]
    public string Carrera { get; set; } = string.Empty;

    [Required, StringLength(30, MinimumLength = 1)]
    public string Ciclo { get; set; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 2)]
    public string DepartamentoPrincipal { get; set; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 2)]
    public string DepartamentoSecundario { get; set; } = string.Empty;

    /// <summary>Obligatoria salvo que se registre con Google (entonces entra con su cuenta de Google).</summary>
    [StringLength(100)]
    public string Password { get; set; } = string.Empty;

    /// <summary>Si se registra con Google: el token del botón. El correo debe ser el de esa cuenta.</summary>
    public string? GoogleCredential { get; set; }
}

/// <summary>Bloque semanal: día (1 = lunes … 7 = domingo) y horas HH:mm de Lima.</summary>
public class BloqueHorarioDto
{
    [Range(1, 7)]
    public int DiaSemana { get; set; }

    [Required, RegularExpression(@"^([01]\d|2[0-3]):[0-5]\d$", ErrorMessage = "Hora inválida (HH:mm)")]
    public string HoraInicio { get; set; } = string.Empty;

    [Required, RegularExpression(@"^([01]\d|2[0-3]):[0-5]\d$", ErrorMessage = "Hora inválida (HH:mm)")]
    public string HoraFin { get; set; } = string.Empty;

    /// <summary>Solo en el horario de prácticas: Presencial o Virtual.</summary>
    [RegularExpression("^(Presencial|Virtual)$", ErrorMessage = "Modalidad no válida")]
    public string? Modalidad { get; set; }

    /// <summary>Sede o plataforma de trabajo.</summary>
    [StringLength(200)]
    public string? Lugar { get; set; }
}

/// <summary>El practicante envía los bloques de su horario de la universidad.</summary>
public class HorarioEnviarDto
{
    [Required, MinLength(1), MaxLength(60)]
    public List<BloqueHorarioDto> Bloques { get; set; } = new();
}

/// <summary>Recursos Humanos pasa el horario a "EnRevision" o lo acepta asignando el horario de prácticas.</summary>
public class HorarioRevisionDto
{
    [Required, RegularExpression("^(EnRevision|Aceptado)$", ErrorMessage = "Estado no válido")]
    public string Estado { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Nota { get; set; }

    /// <summary>Obligatorio al aceptar: el horario de prácticas que se le asigna.</summary>
    public List<BloqueHorarioDto>? HorarioPracticas { get; set; }
}

public class PracticanteDto
{
    public int UsuarioId { get; set; }
    public string Dni { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Apellidos { get; set; } = string.Empty;
    public string? Correo { get; set; }
    public string? Telefono { get; set; }
    public string Universidad { get; set; } = string.Empty;
    public string Carrera { get; set; } = string.Empty;
    public string Ciclo { get; set; } = string.Empty;
    public string DepartamentoPrincipal { get; set; } = string.Empty;
    public string DepartamentoSecundario { get; set; } = string.Empty;
    public string EstadoHorario { get; set; } = string.Empty;
    public string? NotaHorario { get; set; }
    public bool HorarioConfirmado { get; set; }
    public DateTime? FechaEnvioHorario { get; set; }
    public bool Activo { get; set; }
    public DateTime FechaRegistro { get; set; }
    public List<BloqueHorarioDto> HorarioUniversidad { get; set; } = new();
    public List<BloqueHorarioDto> HorarioPracticas { get; set; } = new();
}
