using System.ComponentModel.DataAnnotations;

namespace CapstonePsicologiaAPI.DTOs;

/// <summary>Texto con el que un administrador responde por correo a un mensaje de contacto.</summary>
public class ContactoResponderDto
{
    [Required, StringLength(3000, MinimumLength = 5)]
    public string Respuesta { get; set; } = string.Empty;
}
