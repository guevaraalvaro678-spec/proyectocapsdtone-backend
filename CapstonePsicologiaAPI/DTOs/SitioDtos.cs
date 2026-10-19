using System.ComponentModel.DataAnnotations;

namespace CapstonePsicologiaAPI.DTOs;

/// <summary>Imagen ya subida: `Id` es lo que se guarda en el logo o en una iniciativa.</summary>
public class ImagenSubidaDto
{
    public int Id { get; set; }
    public string Url { get; set; } = string.Empty;
}

public class IniciativaDto
{
    public int Id { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string Texto { get; set; } = string.Empty;
    public string Icono { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public int? ImagenId { get; set; }
    public string? ImagenUrl { get; set; }
    public int Orden { get; set; }
    public bool Activa { get; set; }

    /// <summary>Pendiente, Aprobada o Rechazada (las que proponen los miembros empiezan pendientes).</summary>
    public string Estado { get; set; } = string.Empty;
    public string? MotivoRechazo { get; set; }

    /// <summary>Quién la propuso; null si la creó el administrador.</summary>
    public string? CreadaPorNombre { get; set; }
    public string? CreadaPorRol { get; set; }
    public DateTime FechaCreacion { get; set; }
}

public class IniciativaGuardarDto
{
    [Required, StringLength(150, MinimumLength = 3)]
    public string Titulo { get; set; } = string.Empty;

    [Required, StringLength(600, MinimumLength = 3)]
    public string Texto { get; set; } = string.Empty;

    [StringLength(10)]
    public string? Icono { get; set; }

    [RegularExpression("^(rojo|verde|naranja|gris)$", ErrorMessage = "Color no válido")]
    public string Color { get; set; } = "rojo";

    public int? ImagenId { get; set; }
    public int Orden { get; set; }
    public bool Activa { get; set; } = true;
}

public class LogoGuardarDto
{
    /// <summary>Null quita el logo y el sitio vuelve a mostrar el texto.</summary>
    public int? ImagenId { get; set; }
}

/// <summary>Datos de Yape para donaciones de dinero. Todo en null quita la sección del sitio.</summary>
public class YapeGuardarDto
{
    [RegularExpression(@"^9\d{8}$", ErrorMessage = "El número de Yape debe ser un celular de 9 dígitos que empiece con 9")]
    public string? Numero { get; set; }

    [StringLength(100)]
    public string? Titular { get; set; }

    public int? QrImagenId { get; set; }
}

/// <summary>Lo que necesita el sitio público: logo, iniciativas activas y datos de Yape.</summary>
public class SitioPublicoDto
{
    public string? LogoUrl { get; set; }
    public List<IniciativaDto> Iniciativas { get; set; } = new();

    /// <summary>Null si la ONG no configuró Yape.</summary>
    public YapeDto? Yape { get; set; }
}

public class YapeDto
{
    public string? Numero { get; set; }

    /// <summary>El número con espacios para leerlo fácil (987 654 321).</summary>
    public string? NumeroVisible { get; set; }

    public string? Titular { get; set; }
    public int? QrImagenId { get; set; }
    public string? QrUrl { get; set; }
}
