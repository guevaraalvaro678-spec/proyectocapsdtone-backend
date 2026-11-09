namespace CapstonePsicologiaAPI.Models;

/// <summary>
/// Intención de donar artículos físicos (la plataforma no recibe dinero). La persona la registra desde el sitio
/// público y la entrega en la sede; el administrador marca si se recibió o se desestimó y, al repartirla,
/// registra a quién se entregó. El teléfono se guarda cifrado.
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

    /// <summary>A quién o a qué actividad se entregaron los artículos (al pasar a Entregada).</summary>
    public string? DestinoEntrega { get; set; }
    public DateTime? FechaEntrega { get; set; }

    public List<DonacionArticulo> Articulos { get; set; } = new();
}

/// <summary>Auditoría de una donación (HU-21): registro, cambios de estado, notas y entrega, con quién y cuándo.</summary>
public class HistorialDonacion
{
    public int Id { get; set; }
    public int DonacionId { get; set; }
    public string Accion { get; set; } = string.Empty;
    public string? Detalle { get; set; }

    /// <summary>Quién lo hizo (null cuando la registró el donante desde el sitio).</summary>
    public int? UsuarioId { get; set; }
    public Usuario? Usuario { get; set; }

    public DateTime Fecha { get; set; } = DateTime.UtcNow;
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

    /// <summary>Ya se repartió: sale del inventario.</summary>
    public const string Entregada = "Entregada";

    /// <summary>Recibida se puede devolver a pendiente y Desestimada reabrir, por si hubo un error.</summary>
    private static readonly Dictionary<string, string[]> Siguientes = new()
    {
        [PendienteRecepcion] = new[] { Recibida, Desestimada },
        [Recibida] = new[] { Entregada, PendienteRecepcion },
        [Desestimada] = new[] { PendienteRecepcion },
    };

    public static List<string> Acciones(string estado) =>
        Siguientes.TryGetValue(estado, out var permitidos) ? permitidos.ToList() : new List<string>();

    public static string Etiqueta(string estado) => estado switch
    {
        PendienteRecepcion => "Pendiente de recepción",
        Recibida => "Recibida",
        Desestimada => "Desestimada",
        Entregada => "Entregada",
        _ => estado,
    };
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
