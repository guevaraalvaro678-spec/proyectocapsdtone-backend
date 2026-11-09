using System.ComponentModel.DataAnnotations;

namespace CapstonePsicologiaAPI.DTOs;

public class RespaldoDto
{
    public string Nombre { get; set; } = string.Empty;
    public DateTime Fecha { get; set; }
    public long TamanoBytes { get; set; }
}

public class RespaldoEstadoDto
{
    /// <summary>En Azure SQL las copias son automáticas y no se listan aquí.</summary>
    public bool EnAzure { get; set; }
    public string? Carpeta { get; set; }
    public int HorasEntreCopias { get; set; }
    public int CopiasAConservar { get; set; }
    public DateTime? ProximaCopia { get; set; }

    /// <summary>De la más reciente a la más antigua.</summary>
    public List<RespaldoDto> Copias { get; set; } = new();
}

public class AnuncioGuardarDto
{
    [Required, StringLength(300, MinimumLength = 5)]
    public string Texto { get; set; } = string.Empty;

    [StringLength(300)]
    public string? Enlace { get; set; }

    [Required, RegularExpression("^(Informativo|Importante)$", ErrorMessage = "Tipo no válido")]
    public string Tipo { get; set; } = "Informativo";

    public DateTime? Desde { get; set; }
    public DateTime? Hasta { get; set; }
    public bool Activo { get; set; } = true;
}

public class AnuncioDto
{
    public int Id { get; set; }
    public string Texto { get; set; } = string.Empty;
    public string? Enlace { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public DateTime Desde { get; set; }
    public DateTime? Hasta { get; set; }
    public bool Activo { get; set; }

    /// <summary>Activo y dentro de su vigencia: es lo que ve el público ahora.</summary>
    public bool Vigente { get; set; }

    /// <summary>Texto para mostrar: "Publicado", "Programado", "Vencido" o "Desactivado".</summary>
    public string Situacion { get; set; } = string.Empty;
}

public class AlertaSeguridadDto
{
    public int Id { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public string Titulo { get; set; } = string.Empty;
    public string Detalle { get; set; } = string.Empty;
    public string? Cuenta { get; set; }
    public DateTime Fecha { get; set; }
    public bool Revisada { get; set; }
}

public class AlertasResumenDto
{
    public int Pendientes { get; set; }
    public List<AlertaSeguridadDto> Alertas { get; set; } = new();
}

/// <summary>Indicadores del proceso (los calcula el procedimiento almacenado sp_IndicadoresProceso).</summary>
public class IndicadoresProcesoDto
{
    public int TotalPostulaciones { get; set; }
    public int Decididas { get; set; }
    public int Aceptadas { get; set; }
    public decimal? TasaAceptacion { get; set; }

    /// <summary>Días promedio desde que llega la postulación hasta la decisión final.</summary>
    public decimal? PromedioDiasDecision { get; set; }

    /// <summary>% de postulaciones que tuvieron su primera respuesta dentro de los 3 días.</summary>
    public decimal? PorcentajeRespuestaATiempo { get; set; }

    /// <summary>Días promedio desde que se acepta a un practicante hasta que su horario queda asignado.</summary>
    public decimal? PromedioDiasHorario { get; set; }

    /// <summary>% de postulaciones con todos los datos que pide su tipo.</summary>
    public decimal? PorcentajeRegistrosCompletos { get; set; }

    /// <summary>% de CV renombrados con el formato institucional (CV_código_APELLIDOS.pdf).</summary>
    public decimal? PorcentajeCvIdentificables { get; set; }
}

public class HistorialDonacionDto
{
    public string Accion { get; set; } = string.Empty;
    public string? Detalle { get; set; }
    public string? Responsable { get; set; }
    public DateTime Fecha { get; set; }
}
