using System.ComponentModel.DataAnnotations;

namespace CapstonePsicologiaAPI.DTOs;

/// <summary>Formulario "Postula a AMAAC Perú". El CV se sube antes (api/archivo) y aquí va su Id.</summary>
public class PostulacionCrearDto
{
    [Required, RegularExpression("^(Voluntario|Practicante)$", ErrorMessage = "Tipo no válido")]
    public string Tipo { get; set; } = string.Empty;

    [Required, RegularExpression(@"^\d{8}$", ErrorMessage = "El DNI debe tener 8 dígitos")]
    public string Dni { get; set; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 2)]
    public string Nombres { get; set; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 2)]
    public string Apellidos { get; set; } = string.Empty;

    [Required]
    public DateTime FechaNacimiento { get; set; }

    [Required, StringLength(200, MinimumLength = 3)]
    public string Direccion { get; set; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 2)]
    public string Distrito { get; set; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 2)]
    public string Provincia { get; set; } = string.Empty;

    [Required, StringLength(30, MinimumLength = 6)]
    public string Telefono { get; set; } = string.Empty;

    [StringLength(150)]
    public string? Universidad { get; set; }

    [StringLength(100)]
    public string? Carrera { get; set; }

    [StringLength(30)]
    public string? Ciclo { get; set; }

    [Required, StringLength(100, MinimumLength = 2)]
    public string Departamento { get; set; } = string.Empty;

    [StringLength(100)]
    public string? DepartamentoSecundario { get; set; }

    [Required(ErrorMessage = "Adjunta tu CV en PDF")]
    public int? CvArchivoId { get; set; }

    public bool ParticipaIniciativa { get; set; }

    [StringLength(600)]
    public string? IniciativaDescripcion { get; set; }
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
    public string Dni { get; set; } = string.Empty;
    public string Nombres { get; set; } = string.Empty;
    public string Apellidos { get; set; } = string.Empty;
    public DateTime FechaNacimiento { get; set; }
    public string Direccion { get; set; } = string.Empty;
    public string Distrito { get; set; } = string.Empty;
    public string Provincia { get; set; } = string.Empty;
    public string Telefono { get; set; } = string.Empty;
    public string? Universidad { get; set; }
    public string? Carrera { get; set; }
    public string? Ciclo { get; set; }
    public string Departamento { get; set; } = string.Empty;
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
    [Required, RegularExpression("^(Programada|Realizada|Cancelada)$", ErrorMessage = "Estado no válido")]
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
