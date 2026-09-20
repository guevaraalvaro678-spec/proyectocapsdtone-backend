using System.ComponentModel.DataAnnotations;

namespace CapstonePsicologiaAPI.DTOs;

/// <summary>Datos con los que una persona se registra como usuario común.</summary>
public class UsuarioRegistroDto
{
    [Required, StringLength(100, MinimumLength = 3)]
    public string Nombre { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(150)]
    public string Correo { get; set; } = string.Empty;

    [Required, MinLength(6), MaxLength(100)]
    public string Password { get; set; } = string.Empty;
}
