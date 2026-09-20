namespace CapstonePsicologiaAPI.DTOs;

public class ContactoDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Correo { get; set; } = string.Empty;
    public string Mensaje { get; set; } = string.Empty;
    public DateTime FechaEnvio { get; set; }
    public bool Leido { get; set; }
}
