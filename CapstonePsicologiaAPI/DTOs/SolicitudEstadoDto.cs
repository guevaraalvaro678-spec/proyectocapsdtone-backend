using System.ComponentModel.DataAnnotations;
using CapstonePsicologiaAPI.Models;

namespace CapstonePsicologiaAPI.DTOs;

/// <summary>Nuevo estado que un administrador asigna a una solicitud.</summary>
public class SolicitudEstadoDto
{
    [Required]
    [RegularExpression(
        $"^({EstadosSolicitud.Pendiente}|{EstadosSolicitud.EnRevision}|{EstadosSolicitud.Asignada}|{EstadosSolicitud.Cerrada})$",
        ErrorMessage = "Estado no válido")]
    public string Estado { get; set; } = string.Empty;

    /// <summary>Obligatorio al pasar la solicitud a "Asignada".</summary>
    public int? PsicologoId { get; set; }
}
