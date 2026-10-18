using System.ComponentModel.DataAnnotations;
using CapstonePsicologiaAPI.Models;

namespace CapstonePsicologiaAPI.DTOs;

/// <summary>Datos que envía un administrador para crear o editar un evento.</summary>
public class EventoGuardarDto
{
    [Required, StringLength(150, MinimumLength = 3)]
    public string Titulo { get; set; } = string.Empty;

    [Required, StringLength(600, MinimumLength = 10)]
    public string Descripcion { get; set; } = string.Empty;

    [Required]
    public DateTime FechaInicio { get; set; }

    [Required, StringLength(150, MinimumLength = 2)]
    public string Lugar { get; set; } = string.Empty;

    [RegularExpression(
        $"^({ModalidadesEvento.Presencial}|{ModalidadesEvento.Virtual}|{ModalidadesEvento.Mixta})$",
        ErrorMessage = "Modalidad no válida")]
    public string Modalidad { get; set; } = ModalidadesEvento.Presencial;

    [StringLength(500)]
    [RegularExpression(@"^(https?://\S+|/api/imagen/\d+)$", ErrorMessage = "La imagen debe ser una dirección http o https, o una imagen subida")]
    public string? ImagenUrl { get; set; }

    public bool Publicado { get; set; } = true;

    [RegularExpression("^(Campana|Evento)$", ErrorMessage = "Tipo no válido")]
    public string Tipo { get; set; } = TiposEvento.Evento;
}
