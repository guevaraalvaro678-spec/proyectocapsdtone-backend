using System.ComponentModel.DataAnnotations;

namespace CapstonePsicologiaAPI.DTOs;

/// <summary>
/// Formulario "Postula a AMAAC Perú". El voluntario solo envía nombres, apellidos y teléfono;
/// el practicante además DNI, nacimiento, dirección, formación, área y CV (lo valida el servicio).
/// </summary>
public class PostulacionCrearDto
{
    [Required, RegularExpression("^(Voluntario|Practicante)$", ErrorMessage = "Tipo no válido")]
    public string Tipo { get; set; } = string.Empty;

    [RegularExpression(@"^\d{8}$", ErrorMessage = "El DNI debe tener 8 dígitos")]
    public string? Dni { get; set; }

    [Required, StringLength(100, MinimumLength = 2)]
    public string Nombres { get; set; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 2)]
    public string Apellidos { get; set; } = string.Empty;

    public DateTime? FechaNacimiento { get; set; }

    [StringLength(200, MinimumLength = 3)]
    public string? Direccion { get; set; }

    [StringLength(100, MinimumLength = 2)]
    public string? Distrito { get; set; }

    [StringLength(100, MinimumLength = 2)]
    public string? Provincia { get; set; }

    [Required(ErrorMessage = "Ingresa tu teléfono celular"), RegularExpression(@"^9\d{8}$", ErrorMessage = "El celular debe tener 9 dígitos y empezar con 9")]
    public string Telefono { get; set; } = string.Empty;

    [StringLength(150)]
    public string? Universidad { get; set; }

    [StringLength(100)]
    public string? Carrera { get; set; }

    [StringLength(30)]
    public string? Ciclo { get; set; }

    [StringLength(100, MinimumLength = 2)]
    public string? Departamento { get; set; }

    [StringLength(100)]
    public string? DepartamentoSecundario { get; set; }

    /// <summary>CV ya subido. En el envío completo no se usa: el archivo llega junto con el formulario.</summary>
    public int? CvArchivoId { get; set; }

    public bool ParticipaIniciativa { get; set; }

    [StringLength(600)]
    public string? IniciativaDescripcion { get; set; }
}

/// <summary>
/// Envío completo del formulario (multipart): datos, CV y, si aún no tiene cuenta,
/// el correo y la contraseña o la credencial de Google para crearla en la misma operación.
/// </summary>
public class PostulacionCompletaDto : PostulacionCrearDto
{
    [EmailAddress, StringLength(150)]
    public string? Correo { get; set; }

    [StringLength(100, MinimumLength = 8)]
    public string? Password { get; set; }

    public string? GoogleCredential { get; set; }

    /// <summary>Al crear la cuenta debe aceptar los términos y el tratamiento de datos (Ley 29733).</summary>
    public bool AceptaTerminos { get; set; }
}

public class PostulacionEnviadaDto
{
    /// <summary>Sesión de la cuenta recién creada o vinculada (null si ya había iniciado sesión).</summary>
    public AdminSesionDto? Sesion { get; set; }

    /// <summary>El correo ya era de un voluntario o practicante: no se postula y va a su portal.</summary>
    public bool EsMiembro { get; set; }

    public PostulacionDto? Postulacion { get; set; }
}

/// <summary>El administrador mueve la postulación de estado (y la asigna a una sesión al agendar la entrevista).</summary>
public class PostulacionEstadoDto
{
    [Required, RegularExpression("^(Recibida|HabilitadaEntrevista|EntrevistaAgendada|EntrevistaRealizada|EnCalificacion|Aceptada|NoAdmitida)$",
        ErrorMessage = "Estado no válido")]
    public string Estado { get; set; } = string.Empty;

    /// <summary>Obligatorio al pasar a EntrevistaAgendada.</summary>
    public int? SesionEntrevistaId { get; set; }

    [StringLength(600)]
    public string? NotaEvaluacion { get; set; }
}

public class PostulacionDto
{
    public int Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public int UsuarioId { get; set; }
    public string Correo { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
    /// <summary>DNI, nacimiento, dirección y área: null en las postulaciones de voluntario.</summary>
    public string? Dni { get; set; }
    public string Nombres { get; set; } = string.Empty;
    public string Apellidos { get; set; } = string.Empty;
    public DateTime? FechaNacimiento { get; set; }
    public string? Direccion { get; set; }
    public string? Distrito { get; set; }
    public string? Provincia { get; set; }
    public string Telefono { get; set; } = string.Empty;
    public string? Universidad { get; set; }
    public string? Carrera { get; set; }
    public string? Ciclo { get; set; }
    public string? Departamento { get; set; }
    public string? DepartamentoSecundario { get; set; }
    public int? CvArchivoId { get; set; }
    public string? CvNombre { get; set; }
    public bool ParticipaIniciativa { get; set; }
    public string? IniciativaDescripcion { get; set; }
    public string Estado { get; set; } = string.Empty;
    public string? NotaEvaluacion { get; set; }
    public DateTime FechaEnvio { get; set; }
    public DateTime FechaActualizacion { get; set; }
    public SesionEntrevistaDto? Sesion { get; set; }

    /// <summary>Cambios de estado (solo en el detalle, no en las listas).</summary>
    public List<HistorialPostulacionDto>? Historial { get; set; }

    /// <summary>Estados a los que puede pasar ahora (el panel muestra un botón por cada uno).</summary>
    public List<string> Acciones { get; set; } = new();
}

/// <summary>Indicadores de la lista de postulaciones (con los mismos filtros de la lista).</summary>
public class PostulacionResumenDto
{
    public int Total { get; set; }
    public int Recibidas { get; set; }
    public int Habilitadas { get; set; }
    public int Agendadas { get; set; }
    public int EnProceso { get; set; }
    public int Aceptadas { get; set; }
    public int NoAdmitidas { get; set; }

    /// <summary>Recibidas + habilitadas por agendar.</summary>
    public int RequierenAtencion { get; set; }
}

public class HistorialPostulacionDto
{
    public string Estado { get; set; } = string.Empty;
    public string? Nota { get; set; }
    public string? Responsable { get; set; }
    public DateTime Fecha { get; set; }
}

public class SesionEntrevistaGuardarDto
{
    [Required, StringLength(150, MinimumLength = 3)]
    public string Nombre { get; set; } = string.Empty;

    [Required]
    public DateTime Inicio { get; set; }

    [Required]
    public DateTime Fin { get; set; }

    [Required, RegularExpression("^(Presencial|Virtual)$", ErrorMessage = "Modalidad no válida")]
    public string Modalidad { get; set; } = "Virtual";

    [Required, StringLength(300, MinimumLength = 3)]
    public string LugarOEnlace { get; set; } = string.Empty;

    [Range(1, 100)]
    public int Cupo { get; set; } = 10;
}

public class SesionEstadoDto
{
    [Required, RegularExpression("^(Realizada|Cancelada)$", ErrorMessage = "Estado no válido")]
    public string Estado { get; set; } = string.Empty;
}

public class SesionEntrevistaDto
{
    public int Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public DateTime Inicio { get; set; }
    public DateTime Fin { get; set; }
    public string Modalidad { get; set; } = string.Empty;
    public string LugarOEnlace { get; set; } = string.Empty;
    public int Cupo { get; set; }
    public int Inscritos { get; set; }
    public string Estado { get; set; } = string.Empty;

    /// <summary>Programada, todavía no termina y le quedan cupos: se le pueden agendar postulantes.</summary>
    public bool Disponible { get; set; }

    public int PorcentajeOcupacion { get; set; }
}

/// <summary>Sesión con la lista de postulantes asignados (detalle del administrador).</summary>
public class SesionEntrevistaDetalleDto : SesionEntrevistaDto
{
    public List<PostulacionDto> Postulantes { get; set; } = new();
}

public class ArchivoSubidoDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
}
