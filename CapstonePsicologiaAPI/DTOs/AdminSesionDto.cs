namespace CapstonePsicologiaAPI.DTOs;

/// <summary>Respuesta del login: datos del administrador más su token JWT.</summary>
public class AdminSesionDto
{
    public int Id { get; set; }
    public string Usuario { get; set; } = string.Empty;
    public string Rol { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
}
