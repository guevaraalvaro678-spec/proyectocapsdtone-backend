namespace CapstonePsicologiaAPI.Models;

/// <summary>Evento o actividad de la organización que se muestra en el banner del sitio público.</summary>
public class Evento
{
    public int Id { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public DateTime FechaInicio { get; set; }
    public string Lugar { get; set; } = string.Empty;
    public string Modalidad { get; set; } = ModalidadesEvento.Presencial;
    public string? ImagenUrl { get; set; }

    /// <summary>Solo los eventos publicados aparecen en el sitio público.</summary>
    public bool Publicado { get; set; } = true;

    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
}

public static class ModalidadesEvento
{
    public const string Presencial = "Presencial";
    public const string Virtual = "Virtual";
    public const string Mixta = "Mixta";
}
