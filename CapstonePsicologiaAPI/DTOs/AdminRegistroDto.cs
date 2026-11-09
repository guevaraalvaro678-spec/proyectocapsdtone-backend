using System.ComponentModel.DataAnnotations;
using CapstonePsicologiaAPI.Models;

namespace CapstonePsicologiaAPI.DTOs;

/// <summary>
/// Cuenta del equipo de gestión (Admin, SuperAdmin o Recursos Humanos) que crea un SuperAdmin.
/// El correo es obligatorio: ahí llegan el código de verificación, las alertas y la recuperación de contraseña.
/// </summary>
public class AdminRegistroDto
{
    [Required(ErrorMessage = "Escribe el nombre"), StringLength(100, MinimumLength = 2, ErrorMessage = "El nombre debe tener entre 2 y 100 caracteres")]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "Escribe el correo"), EmailAddress(ErrorMessage = "Correo no válido"), StringLength(150)]
    public string Correo { get; set; } = string.Empty;

    [Required, MinLength(8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres")]
    public string Password { get; set; } = string.Empty;

    [RegularExpression($"^({Roles.Admin}|{Roles.SuperAdmin}|{Roles.RRHH})$", ErrorMessage = "Rol no válido")]
    public string Rol { get; set; } = Roles.Admin;
}
