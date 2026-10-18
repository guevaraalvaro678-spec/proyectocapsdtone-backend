namespace CapstonePsicologiaAPI.Models;

/// <summary>
/// Intención de donar artículos físicos (la plataforma no recibe dinero). La persona la registra desde el sitio
/// público y la entrega en la sede; el administrador marca si se recibió o se desestimó.
/// </summary>
public class Donacion
{
    public int Id { get; set; }

    /// <summary>Código visible, por ejemplo DON-2026-001.</summary>
    public string Codigo { get; set; } = string.Empty;

    public string DonanteNombre { get; set; } = string.Empty;
    public string Telefono { get; set; } = string.Empty;
    public string Estado { get; set; } = EstadosDonacion.PendienteRecepcion;
    public string? Observaciones { get; set; }
    public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;
    public DateTime? FechaRevision { get; set; }

    public List<DonacionArticulo> Articulos { get; set; } = new();
}

/// <summary>Un artículo dentro de una donación (por ejemplo, 50 cuadernos A4).</summary>
public class DonacionArticulo
{
    public int Id { get; set; }
    public int DonacionId { get; set; }
    public string Categoria { get; set; } = CategoriasDonacion.Otros;
    public string Nombre { get; set; } = string.Empty;
    public int Cantidad { get; set; } = 1;
}

public static class EstadosDonacion
{
    public const string PendienteRecepcion = "PendienteRecepcion";
    public const string Recibida = "Recibida";
    public const string Desestimada = "Desestimada";
}

public static class CategoriasDonacion
{
    public const string MaterialEducativo = "MaterialEducativo";
    public const string RopaAbrigo = "RopaAbrigo";
    public const string Juguetes = "Juguetes";
    public const string Alimentos = "Alimentos";
    public const string Higiene = "Higiene";
    public const string Otros = "Otros";

    public static readonly string[] Todas = { MaterialEducativo, RopaAbrigo, Juguetes, Alimentos, Higiene, Otros };
}

/// <summary>Noticia que se publica en el sitio público.</summary>
public class Noticia
{
    public int Id { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string Resumen { get; set; } = string.Empty;
    public string? Contenido { get; set; }
    public int? ImagenId { get; set; }
    public Imagen? Imagen { get; set; }
    public string Estado { get; set; } = EstadosNoticia.Borrador;
    public DateTime Fecha { get; set; } = DateTime.UtcNow;
}

public static class EstadosNoticia
{
    public const string Borrador = "Borrador";
    public const string Publicada = "Publicada";
    public const string NoPublicada = "NoPublicada";
}
