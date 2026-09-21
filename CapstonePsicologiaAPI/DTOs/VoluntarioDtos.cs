using System.ComponentModel.DataAnnotations;

namespace CapstonePsicologiaAPI.DTOs;

/// <summary>Registro público de un voluntario: solo lo esencial, todo obligatorio.</summary>
public class VoluntarioRegistroDto
{
    [Required, RegularExpression(@"^\d{8}$", ErrorMessage = "El DNI debe tener 8 dígitos")]
    public string Dni { get; set; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 2)]
    public string Nombres { get; set; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 2)]
    public string Apellidos { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(150)]
    public string Correo { get; set; } = string.Empty;

    [Required, StringLength(30, MinimumLength = 6)]
    public string Telefono { get; set; } = string.Empty;

    /// <summary>Si se registra con Google: el token del botón. El correo debe ser el de esa cuenta.</summary>
    public string? GoogleCredential { get; set; }
}

public class VoluntarioDto
{
    public int UsuarioId { get; set; }
    public string Dni { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Apellidos { get; set; } = string.Empty;
    public string? Correo { get; set; }
    public string? Telefono { get; set; }
    public bool Activo { get; set; }
    public DateTime FechaRegistro { get; set; }
}
