namespace CapstonePsicologiaAPI.Models;

/// <summary>
/// Perfil de un practicante. Envía su horario de la universidad y Recursos Humanos, tras revisarlo,
/// le asigna su horario de prácticas.
/// </summary>
public class Practicante
{
    /// <summary>DNI de 8 dígitos: clave primaria.</summary>
    public string Dni { get; set; } = string.Empty;

    public int UsuarioId { get; set; }
    public Usuario? Usuario { get; set; }

    public string Apellidos { get; set; } = string.Empty;
    public string Universidad { get; set; } = string.Empty;
    public string Carrera { get; set; } = string.Empty;
    public string Ciclo { get; set; } = string.Empty;
    public string DepartamentoPrincipal { get; set; } = string.Empty;
    public string DepartamentoSecundario { get; set; } = string.Empty;

    public string EstadoHorario { get; set; } = EstadosHorario.SinEnviar;
    public string? NotaHorario { get; set; }

    /// <summary>El practicante confirmó el horario de prácticas asignado.</summary>
    public bool HorarioConfirmado { get; set; }
    public DateTime? FechaEnvioHorario { get; set; }
    public DateTime? FechaRevisionHorario { get; set; }
    public int? RevisadoPorUsuarioId { get; set; }

    public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;
}

public static class EstadosHorario
{
    public const string SinEnviar = "SinEnviar";
    public const string Pendiente = "Pendiente";
    public const string EnRevision = "EnRevision";
    public const string Aceptado = "Aceptado";
}

/// <summary>Bloque semanal de horario. Tipo Universidad lo envía el practicante; Practicas lo asigna Recursos Humanos.</summary>
public class BloqueHorario
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }
    public Usuario? Usuario { get; set; }

    public string Tipo { get; set; } = TiposBloque.Universidad;

    /// <summary>1 = lunes … 7 = domingo.</summary>
    public int DiaSemana { get; set; }

    /// <summary>Formato HH:mm, hora de Lima.</summary>
    public string HoraInicio { get; set; } = string.Empty;
    public string HoraFin { get; set; } = string.Empty;

    /// <summary>Solo en el horario de prácticas: Presencial o Virtual, y la sede o plataforma.</summary>
    public string? Modalidad { get; set; }
    public string? Lugar { get; set; }
}

public static class TiposBloque
{
    public const string Universidad = "Universidad";
    public const string Practicas = "Practicas";
}

/// <summary>Marcación de asistencia del practicante: entrada y, al terminar, salida.</summary>
public class Marcacion
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }
    public Usuario? Usuario { get; set; }
    public DateTime Entrada { get; set; } = DateTime.UtcNow;
    public DateTime? Salida { get; set; }
}
