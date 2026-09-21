using System.ComponentModel.DataAnnotations;

namespace CapstonePsicologiaAPI.DTOs;

/// <summary>Postulación pública de un psicólogo colaborador ("Trabaja con nosotros").</summary>
public class ColaboradorPostulacionDto
{
    [Required, RegularExpression(@"^\d{8}$", ErrorMessage = "El DNI debe tener 8 dígitos")]
    public string Dni { get; set; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 2)]
    public string Nombre { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(150)]
    public string Correo { get; set; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 6)]
    public string Password { get; set; } = string.Empty;

    [StringLength(30)]
    public string? Telefono { get; set; }

    [Required, StringLength(100, MinimumLength = 2)]
    public string Especialidad { get; set; } = string.Empty;

    [StringLength(30)]
    public string? NumeroColegiatura { get; set; }

    [StringLength(1000)]
    public string? Presentacion { get; set; }
}

/// <summary>Solicitud de colaborador tal como la ve el administrador.</summary>
public class ColaboradorDto
{
    public int Id { get; set; }
    public string? Dni { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Correo { get; set; } = string.Empty;
    public string? Telefono { get; set; }
    public string Especialidad { get; set; } = string.Empty;
    public string? NumeroColegiatura { get; set; }
    public string? Presentacion { get; set; }
    public string Estado { get; set; } = string.Empty;
    public string? MotivoRechazo { get; set; }
    public DateTime FechaSolicitud { get; set; }
    public DateTime? FechaRevision { get; set; }
    public int? UsuarioId { get; set; }
}

public class ColaboradorRechazoDto
{
    [StringLength(500)]
    public string? Motivo { get; set; }
}
