using System.ComponentModel.DataAnnotations;

namespace CapstonePsicologiaAPI.DTOs;

/// <summary>"Crea tu cuenta": solo correo y contraseña. La cuenta nace como Postulante.</summary>
public class CuentaRegistroDto
{
    [Required, EmailAddress, StringLength(150)]
    public string Correo { get; set; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres")]
    public string Password { get; set; } = string.Empty;
}

/// <summary>Datos de la cuenta que inició sesión ("Mi cuenta y seguridad").</summary>
public class CuentaDto
{
    public int Id { get; set; }
    public string Usuario { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string? Correo { get; set; }
    public string? Telefono { get; set; }
    public string Rol { get; set; } = string.Empty;
    public bool Activo { get; set; }
    public string? Dni { get; set; }
    public DateTime FechaRegistro { get; set; }
}

public class CuentaPerfilDto
{
    [Required, StringLength(100, MinimumLength = 2)]
    public string Nombre { get; set; } = string.Empty;

    [StringLength(30)]
    public string? Telefono { get; set; }
}

public class CuentaPasswordDto
{
    [Required]
    public string Actual { get; set; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 8, ErrorMessage = "La nueva contraseña debe tener al menos 8 caracteres")]
    public string Nueva { get; set; } = string.Empty;
}

/// <summary>Propuesta de iniciativa de un voluntario o practicante ("Mis iniciativas").</summary>
public class IniciativaPropuestaDto
{
    [Required, StringLength(150, MinimumLength = 3)]
    public string Titulo { get; set; } = string.Empty;

    [Required, StringLength(600, MinimumLength = 10)]
    public string Texto { get; set; } = string.Empty;
}

public class IniciativaRevisionDto
{
    [Required, RegularExpression("^(Aprobada|Rechazada)$", ErrorMessage = "Estado no válido")]
    public string Estado { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Motivo { get; set; }
}

/// <summary>Indicadores del dashboard de la ONG.</summary>
public class DashboardOngDto
{
    /// <summary>Cantidad de postulaciones por estado (clave = estado).</summary>
    public Dictionary<string, int> Postulaciones { get; set; } = new();
    public int TotalPostulaciones { get; set; }
    public int Voluntarios { get; set; }
    public int Practicantes { get; set; }
    public int PersonalAdministrativo { get; set; }
    public Dictionary<string, int> Donaciones { get; set; } = new();
    public int IniciativasPendientes { get; set; }
    public int HorariosPendientes { get; set; }
    public List<SesionEntrevistaDto> ProximasSesiones { get; set; } = new();

    // ---- Indicadores ya calculados ----

    /// <summary>Etapas del proceso: cuántas postulaciones llegaron a cada una (o más allá).</summary>
    public List<EtapaEmbudoDto> Embudo { get; set; } = new();

    /// <summary>Recibidas + habilitadas por agendar.</summary>
    public int PendientesAtencion { get; set; }

    /// <summary>Entrevista realizada + en calificación.</summary>
    public int EnEvaluacion { get; set; }

    public int Miembros { get; set; }
    public int PorcentajeVoluntarios { get; set; }
    public ResumenDonacionesDto ResumenDonaciones { get; set; } = new();
}

public class EtapaEmbudoDto
{
    public string Etiqueta { get; set; } = string.Empty;
    public int Valor { get; set; }
    public int Porcentaje { get; set; }
    public bool Alerta { get; set; }
}

public class ResumenDonacionesDto
{
    public int Total { get; set; }
    public int Pendientes { get; set; }
    public int Recibidas { get; set; }
    public int Desestimadas { get; set; }
    public decimal PorcentajePendientes { get; set; }
    public decimal PorcentajeRecibidas { get; set; }
    public decimal PorcentajeDesestimadas { get; set; }
}
