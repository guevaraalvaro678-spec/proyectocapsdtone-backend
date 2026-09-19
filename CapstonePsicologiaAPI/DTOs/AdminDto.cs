namespace CapstonePsicologiaAPI.DTOs;

/// <summary>Datos de un administrador que se devuelven al front (nunca incluye la contraseña).</summary>
public class AdminDto
{
    public int Id { get; set; }
    public string Usuario { get; set; } = string.Empty;
    public string Rol { get; set; } = string.Empty;
}
