using System.ComponentModel.DataAnnotations;

namespace CapstonePsicologiaAPI.DTOs;

public class GoogleCredencialDto
{
    [Required]
    public string Credential { get; set; } = string.Empty;
}

public class GooglePerfilDto
{
    public string Correo { get; set; } = string.Empty;
    public string Nombres { get; set; } = string.Empty;
    public string Apellidos { get; set; } = string.Empty;
}

public class GoogleConfigDto
{
    /// <summary>Null cuando el acceso con Google no está configurado: el front esconde los botones.</summary>
    public string? ClientId { get; set; }
}
