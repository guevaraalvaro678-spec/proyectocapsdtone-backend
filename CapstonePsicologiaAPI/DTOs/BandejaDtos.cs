namespace CapstonePsicologiaAPI.DTOs;

/// <summary>Bandeja de trabajo de Recursos Humanos: lo que espera una acción hoy.</summary>
public class BandejaRrhhDto
{
    /// <summary>Postulaciones recibidas que nadie revisó todavía.</summary>
    public int PorRevisar { get; set; }

    /// <summary>Habilitadas a las que falta asignar una sesión de entrevista.</summary>
    public int PorAgendar { get; set; }

    /// <summary>Ya entrevistadas: falta aceptar o no admitir.</summary>
    public int PorDecidir { get; set; }

    /// <summary>Postulaciones abiertas que llevan más de <see cref="DiasLimiteRespuesta"/> días sin moverse.</summary>
    public int Atrasadas { get; set; }
    public int DiasLimiteRespuesta { get; set; }

    /// <summary>Sesiones que ya terminaron y siguen como programadas (hay que marcarlas realizadas).</summary>
    public int SesionesPorCerrar { get; set; }
    public int HorariosPorRevisar { get; set; }
    public int TareasVencidas { get; set; }

    /// <summary>Practicantes con la entrada marcada y sin salida.</summary>
    public int EnTurnoAhora { get; set; }

    /// <summary>Las postulaciones que más tiempo llevan esperando, con el siguiente paso sugerido.</summary>
    public List<PendienteBandejaDto> Postulaciones { get; set; } = new();
    public List<SesionEntrevistaDto> Sesiones { get; set; } = new();
    public List<PendienteBandejaDto> Horarios { get; set; } = new();
    public List<PendienteBandejaDto> Tareas { get; set; } = new();
}

public class PendienteBandejaDto
{
    public int Id { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string Detalle { get; set; } = string.Empty;

    /// <summary>Desde cuándo espera (UTC).</summary>
    public DateTime Fecha { get; set; }
    public int DiasEspera { get; set; }

    /// <summary>Pasó el plazo de respuesta o la fecha límite.</summary>
    public bool Atrasado { get; set; }
}

/// <summary>El mismo cambio de estado para varias postulaciones a la vez.</summary>
public class PostulacionEstadoMasivoDto : PostulacionEstadoDto
{
    [System.ComponentModel.DataAnnotations.Required,
     System.ComponentModel.DataAnnotations.MinLength(1, ErrorMessage = "Elige al menos una postulación"),
     System.ComponentModel.DataAnnotations.MaxLength(100, ErrorMessage = "Máximo 100 postulaciones por vez")]
    public List<int> Ids { get; set; } = new();
}

public class ResultadoMasivoDto
{
    public int Procesadas { get; set; }
    public List<ErrorMasivoDto> Errores { get; set; } = new();
}

public class ErrorMasivoDto
{
    public int Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Mensaje { get; set; } = string.Empty;
}
