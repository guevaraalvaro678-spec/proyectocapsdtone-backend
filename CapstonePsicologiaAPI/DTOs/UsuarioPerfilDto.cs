namespace CapstonePsicologiaAPI.DTOs;

/// <summary>Perfil del usuario que se muestra en "Mi espacio".</summary>
public class UsuarioPerfilDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Correo { get; set; }
    public string? Ubicacion { get; set; }
    public string? Intereses { get; set; }
    public bool RecordatoriosCorreo { get; set; }
    public bool PreferenciaVirtual { get; set; }
    public DateTime FechaRegistro { get; set; }
}
