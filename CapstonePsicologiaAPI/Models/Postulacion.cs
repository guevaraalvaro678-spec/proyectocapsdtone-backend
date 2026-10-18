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

    // ---- Datos personales ----
    public string Dni { get; set; } = string.Empty;
    public string Nombres { get; set; } = string.Empty;
    public string Apellidos { get; set; } = string.Empty;
    public DateTime FechaNacimiento { get; set; }
    public string Direccion { get; set; } = string.Empty;
    public string Distrito { get; set; } = string.Empty;
    public string Provincia { get; set; } = string.Empty;
    public string Telefono { get; set; } = string.Empty;

    // ---- Formación académica (obligatoria para practicantes) ----
    public string? Universidad { get; set; }
    public string? Carrera { get; set; }
    public string? Ciclo { get; set; }

    // ---- Área y CV ----
    public string Departamento { get; set; } = string.Empty;
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
