namespace CapstonePsicologiaAPI.Models;

/// <summary>Mensaje enviado desde el formulario de contacto del sitio público.</summary>
public class Contacto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Correo { get; set; } = string.Empty;
    public string Mensaje { get; set; } = string.Empty;
    public DateTime FechaEnvio { get; set; } = DateTime.UtcNow;
    public bool Leido { get; set; }
}
