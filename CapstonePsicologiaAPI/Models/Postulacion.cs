namespace CapstonePsicologiaAPI.Models;

/// <summary>
/// Postulación de una persona (con cuenta de rol Postulante) para ser voluntaria o practicante.
/// Recorre: Recibida → HabilitadaEntrevista → EntrevistaAgendada → EntrevistaRealizada → EnCalificacion → Aceptada / NoAdmitida.
/// Al aceptarla, la cuenta pasa a Voluntario o Practicante y se crea su perfil.
/// </summary>
public class Postulacion
{
    public int Id { get; set; }

    /// <summary>Código visible, por ejemplo POS-2026-001.</summary>
    public string Codigo { get; set; } = string.Empty;

    public int UsuarioId { get; set; }
    public Usuario? Usuario { get; set; }

    /// <summary>Voluntario o Practicante.</summary>
    public string Tipo { get; set; } = TiposPostulacion.Voluntario;

    // ---- Datos personales (al voluntario solo se le piden nombres, apellidos y teléfono) ----
    public string? Dni { get; set; }
    public string Nombres { get; set; } = string.Empty;
    public string Apellidos { get; set; } = string.Empty;
    public DateTime? FechaNacimiento { get; set; }
    public string? Direccion { get; set; }
    public string? Distrito { get; set; }
    public string? Provincia { get; set; }
    public string Telefono { get; set; } = string.Empty;

    // ---- Formación académica (obligatoria para practicantes) ----
    public string? Universidad { get; set; }
    public string? Carrera { get; set; }
    public string? Ciclo { get; set; }

    // ---- Área y CV (solo practicantes) ----
    public string? Departamento { get; set; }
    public string? DepartamentoSecundario { get; set; }
    public int? CvArchivoId { get; set; }
    public Archivo? CvArchivo { get; set; }

    // ---- Iniciativas ----
    public bool ParticipaIniciativa { get; set; }
    public string? IniciativaDescripcion { get; set; }

    // ---- Proceso ----
    public string Estado { get; set; } = EstadosPostulacion.Recibida;
    public int? SesionEntrevistaId { get; set; }
    public SesionEntrevista? SesionEntrevista { get; set; }

    /// <summary>Comentario de la evaluación; se le muestra al postulante con el resultado.</summary>
    public string? NotaEvaluacion { get; set; }

    public DateTime FechaEnvio { get; set; } = DateTime.UtcNow;
    public DateTime FechaActualizacion { get; set; } = DateTime.UtcNow;
}

/// <summary>Áreas (departamentos) a las que se puede postular. El formulario las pide a GET api/catalogo/areas.</summary>
public static class AreasOng
{
    public static readonly string[] Todas =
    {
        "Educación y Desarrollo Comunitario",
        "Gestión Social",
        "Comunicaciones",
        "Proyectos Sociales",
        "Eventos",
        "Recursos Humanos",
        "Administración y Finanzas",
        "Tecnología",
    };

    public static bool Existe(string? area) => area is not null && Todas.Contains(area.Trim());
}

public static class TiposPostulacion
{
    public const string Voluntario = "Voluntario";
    public const string Practicante = "Practicante";
}

public static class EstadosPostulacion
{
    public const string Recibida = "Recibida";
    public const string HabilitadaEntrevista = "HabilitadaEntrevista";
    public const string EntrevistaAgendada = "EntrevistaAgendada";
    public const string EntrevistaRealizada = "EntrevistaRealizada";
    public const string EnCalificacion = "EnCalificacion";
    public const string Aceptada = "Aceptada";
    public const string NoAdmitida = "NoAdmitida";

    public static readonly string[] Todos =
        { Recibida, HabilitadaEntrevista, EntrevistaAgendada, EntrevistaRealizada, EnCalificacion, Aceptada, NoAdmitida };

    public static bool EsFinal(string estado) => estado is Aceptada or NoAdmitida;

    /// <summary>Pasos permitidos del proceso. "No admitida" se puede dar en cualquier etapa abierta.</summary>
    private static readonly Dictionary<string, string[]> Siguientes = new()
    {
        [Recibida] = new[] { HabilitadaEntrevista },
        [HabilitadaEntrevista] = new[] { EntrevistaAgendada },
        // Volver a "Entrevista agendada" estando agendada = cambiar de sesión.
        [EntrevistaAgendada] = new[] { EntrevistaAgendada, EntrevistaRealizada },
        [EntrevistaRealizada] = new[] { EnCalificacion },
        [EnCalificacion] = new[] { Aceptada },
    };

    /// <summary>Estados a los que puede pasar una postulación que está en <paramref name="estado"/>.</summary>
    public static List<string> Acciones(string estado) =>
        EsFinal(estado) || !Siguientes.TryGetValue(estado, out var permitidos)
            ? new List<string>()
            : permitidos.Append(NoAdmitida).ToList();

    public static bool PuedePasar(string desde, string hacia) => Acciones(desde).Contains(hacia);

    /// <summary>Texto para mostrar cada estado en mensajes y correos.</summary>
    public static string Etiqueta(string estado) => estado switch
    {
        Recibida => "Recibida",
        HabilitadaEntrevista => "Habilitada para entrevista",
        EntrevistaAgendada => "Entrevista agendada",
        EntrevistaRealizada => "Entrevista realizada",
        EnCalificacion => "En proceso de calificación",
        Aceptada => "Aceptada",
        NoAdmitida => "No admitida",
        _ => estado,
    };
}

/// <summary>Cada cambio de estado de una postulación: qué estado tomó, cuándo y quién lo hizo.</summary>
public class HistorialPostulacion
{
    public int Id { get; set; }
    public int PostulacionId { get; set; }
    public string Estado { get; set; } = string.Empty;
    public string? Nota { get; set; }

    /// <summary>Quién hizo el cambio (null si fue automático, por ejemplo al cerrar una sesión).</summary>
    public int? UsuarioId { get; set; }
    public Usuario? Usuario { get; set; }

    public DateTime Fecha { get; set; } = DateTime.UtcNow;
}

/// <summary>Sesión de entrevista grupal a la que se asignan varios postulantes (con un cupo máximo).</summary>
public class SesionEntrevista
{
    public int Id { get; set; }

    /// <summary>Código visible, por ejemplo SES-001.</summary>
    public string Codigo { get; set; } = string.Empty;

    public string Nombre { get; set; } = string.Empty;
    public DateTime Inicio { get; set; }
    public DateTime Fin { get; set; }

    /// <summary>Presencial o Virtual.</summary>
    public string Modalidad { get; set; } = ModalidadesEvento.Virtual;

    /// <summary>Dirección o enlace de la reunión.</summary>
    public string LugarOEnlace { get; set; } = string.Empty;

    public int Cupo { get; set; } = 10;
    public string Estado { get; set; } = EstadosSesion.Programada;
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
}

public static class EstadosSesion
{
    public const string Programada = "Programada";
    public const string Realizada = "Realizada";
    public const string Cancelada = "Cancelada";
}

/// <summary>Archivo subido (por ahora, el CV en PDF de las postulaciones). Se guarda en la base.</summary>
public class Archivo
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string TipoContenido { get; set; } = string.Empty;
    public byte[] Datos { get; set; } = Array.Empty<byte>();
    public int SubidoPorUsuarioId { get; set; }
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
}
