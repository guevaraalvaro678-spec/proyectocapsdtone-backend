namespace CapstonePsicologiaAPI.Models;

/// <summary>Imagen subida por el administrador. Se guarda en la base para que todo el equipo vea las mismas imágenes.</summary>
public class Imagen
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string TipoContenido { get; set; } = string.Empty;
    public byte[] Datos { get; set; } = Array.Empty<byte>();
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
}

/// <summary>Iniciativa o taller que se muestra en el sitio público.</summary>
public class Iniciativa
{
    public int Id { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string Texto { get; set; } = string.Empty;

    /// <summary>Emoji que se muestra cuando la iniciativa no tiene imagen.</summary>
    public string Icono { get; set; } = "⭐";

    /// <summary>rojo, verde, naranja o gris: color del borde de la tarjeta.</summary>
    public string Color { get; set; } = ColoresIniciativa.Rojo;

    public int? ImagenId { get; set; }
    public Imagen? Imagen { get; set; }

    public int Orden { get; set; }
    public bool Activa { get; set; } = true;
}

public static class ColoresIniciativa
{
    public const string Rojo = "rojo";
    public const string Verde = "verde";
    public const string Naranja = "naranja";
    public const string Gris = "gris";
}

/// <summary>Ajustes del sitio público (una sola fila, Id = 1): por ahora el logo.</summary>
public class SitioConfig
{
    public int Id { get; set; } = 1;
    public int? LogoImagenId { get; set; }
    public Imagen? LogoImagen { get; set; }
}
