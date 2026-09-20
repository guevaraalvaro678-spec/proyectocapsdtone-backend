using System.ComponentModel.DataAnnotations;

namespace CapstonePsicologiaAPI.DTOs;

/// <summary>Lo que escribe el usuario al pedir orientación.</summary>
public class SolicitudCrearDto
{
    [Required, StringLength(1000, MinimumLength = 10)]
    public string Motivo { get; set; } = string.Empty;
}
