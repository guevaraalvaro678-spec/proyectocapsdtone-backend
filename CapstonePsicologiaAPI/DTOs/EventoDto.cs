namespace CapstonePsicologiaAPI.DTOs;

public class EventoDto
{
    public int Id { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public DateTime FechaInicio { get; set; }
    public string Lugar { get; set; } = string.Empty;
    public string Modalidad { get; set; } = string.Empty;
    public string? ImagenUrl { get; set; }
    public bool Publicado { get; set; }
}
