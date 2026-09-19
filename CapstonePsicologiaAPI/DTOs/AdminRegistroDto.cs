using System.ComponentModel.DataAnnotations;
using CapstonePsicologiaAPI.Models;

namespace CapstonePsicologiaAPI.DTOs;

/// <summary>Datos que envía el front para crear un nuevo administrador.</summary>
public class AdminRegistroDto
{
    [Required, StringLength(50, MinimumLength = 3)]
    public string Usuario { get; set; } = string.Empty;

    [Required, MinLength(3)]
    public string Password { get; set; } = string.Empty;

    [RegularExpression($"^({Roles.Admin}|{Roles.SuperAdmin})$", ErrorMessage = "Rol no válido")]
    public string Rol { get; set; } = Roles.Admin;
}
