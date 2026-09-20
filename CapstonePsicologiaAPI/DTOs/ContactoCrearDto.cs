using System.ComponentModel.DataAnnotations;

namespace CapstonePsicologiaAPI.DTOs;

/// <summary>Datos que envía el formulario de contacto.</summary>
public class ContactoCrearDto
{
    [Required, StringLength(100, MinimumLength = 3)]
    public string Nombre { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(150)]
    public string Correo { get; set; } = string.Empty;

    [Required, StringLength(1000, MinimumLength = 10)]
    public string Mensaje { get; set; } = string.Empty;
}
