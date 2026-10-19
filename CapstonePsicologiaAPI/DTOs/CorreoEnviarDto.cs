using System.ComponentModel.DataAnnotations;

namespace CapstonePsicologiaAPI.DTOs;

/// <summary>Correo o notificación que redacta un administrador para uno o muchos destinatarios.</summary>
public class CorreoEnviarDto : IValidatableObject
{
    /// <summary>Usuarios, Psicologos, Administradores, Todos o Individual.</summary>
    [Required]
    [RegularExpression("^(Usuarios|Psicologos|Postulantes|Administradores|Todos|Individual)$", ErrorMessage = "Audiencia no válida")]
    public string Audiencia { get; set; } = string.Empty;

    /// <summary>Solo para la audiencia "Individual".</summary>
    [EmailAddress, StringLength(150)]
    public string? Correo { get; set; }

    [Required, StringLength(150, MinimumLength = 3)]
    public string Asunto { get; set; } = string.Empty;

    [Required, StringLength(5000, MinimumLength = 5)]
    public string Mensaje { get; set; } = string.Empty;

    /// <summary>Por defecto los usuarios que apagaron los correos no lo reciben; esto lo fuerza.</summary>
    public bool IncluirSinPreferencia { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext contexto)
    {
        if (Audiencia == "Individual" && string.IsNullOrWhiteSpace(Correo))
            yield return new ValidationResult("Indica el correo del destinatario", new[] { nameof(Correo) });
    }
}

public class CorreoEnvioResultadoDto
{
    public int Destinatarios { get; set; }
    public string Lote { get; set; } = string.Empty;
}

public class CorreoEstadoDto
{
    /// <summary>False si el servidor no tiene SMTP configurado: los correos se guardan pero no salen.</summary>
    public bool SmtpConfigurado { get; set; }
    public int Pendientes { get; set; }
}
