using System.ComponentModel.DataAnnotations;

namespace CapstonePsicologiaAPI.DTOs;

public class DonacionArticuloDto
{
    [Required, RegularExpression("^(MaterialEducativo|RopaAbrigo|Juguetes|Alimentos|Higiene|Otros)$", ErrorMessage = "Categoría no válida")]
    public string Categoria { get; set; } = "Otros";

    [Required, StringLength(150, MinimumLength = 2)]
    public string Nombre { get; set; } = string.Empty;

    [Range(1, 10000)]
    public int Cantidad { get; set; } = 1;
}

/// <summary>Registro público de una donación de artículos.</summary>
public class DonacionCrearDto
{
    [Required, StringLength(150, MinimumLength = 3)]
    public string DonanteNombre { get; set; } = string.Empty;

    [Required, StringLength(30, MinimumLength = 6)]
    public string Telefono { get; set; } = string.Empty;

    [Required, MinLength(1, ErrorMessage = "Agrega al menos un artículo"), MaxLength(50)]
    public List<DonacionArticuloDto> Articulos { get; set; } = new();
}

public class DonacionEstadoDto
{
    [Required, RegularExpression("^(PendienteRecepcion|Recibida|Desestimada)$", ErrorMessage = "Estado no válido")]
    public string Estado { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Observaciones { get; set; }
}

public class DonacionDto
{
    public int Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string DonanteNombre { get; set; } = string.Empty;
    public string Telefono { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public string? Observaciones { get; set; }
    public DateTime FechaRegistro { get; set; }
    public List<DonacionArticuloDto> Articulos { get; set; } = new();
}

public class DonacionObservacionesDto
{
    [StringLength(500)]
    public string? Observaciones { get; set; }
}

/// <summary>Panel de donaciones: totales por estado e inventario de lo recibido.</summary>
public class DonacionResumenDto
{
    public int Pendientes { get; set; }
    public int Recibidas { get; set; }
    public int Desestimadas { get; set; }
    public int Total { get; set; }

    /// <summary>Artículos de las donaciones recibidas, sumados por categoría y nombre.</summary>
    public List<DonacionArticuloDto> Inventario { get; set; } = new();
}

public class NoticiaGuardarDto
{
    [Required, StringLength(200, MinimumLength = 3)]
    public string Titulo { get; set; } = string.Empty;

    [Required, StringLength(400, MinimumLength = 3)]
    public string Resumen { get; set; } = string.Empty;

    [StringLength(4000)]
    public string? Contenido { get; set; }

    public int? ImagenId { get; set; }

    [Required, RegularExpression("^(Borrador|Publicada|NoPublicada)$", ErrorMessage = "Estado no válido")]
    public string Estado { get; set; } = "Borrador";

    public DateTime? Fecha { get; set; }
}

public class NoticiaDto
{
    public int Id { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string Resumen { get; set; } = string.Empty;
    public string? Contenido { get; set; }
    public int? ImagenId { get; set; }
    public string? ImagenUrl { get; set; }
    public string Estado { get; set; } = string.Empty;
    public DateTime Fecha { get; set; }
}
