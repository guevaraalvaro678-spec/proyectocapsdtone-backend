namespace CapstonePsicologiaAPI.DTOs;

/// <summary>Respuesta del login o del registro: datos de la cuenta más su token JWT.</summary>
public class AdminSesionDto
{
    public int Id { get; set; }
    public string Usuario { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Rol { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
}
