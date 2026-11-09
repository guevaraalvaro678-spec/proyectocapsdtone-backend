namespace CapstonePsicologiaAPI.Models;

/// <summary>Aviso que se muestra en la franja superior del sitio público mientras esté vigente (HU-23).</summary>
public class Anuncio
{
    public int Id { get; set; }
    public string Texto { get; set; } = string.Empty;

    /// <summary>Opcional: a dónde lleva el botón "Ver más" (ruta del sitio o dirección completa).</summary>
    public string? Enlace { get; set; }

    public string Tipo { get; set; } = TiposAnuncio.Informativo;

    /// <summary>Vigencia en UTC. Sin fecha de fin queda publicado hasta que se desactive.</summary>
    public DateTime Desde { get; set; } = DateTime.UtcNow;
    public DateTime? Hasta { get; set; }

    public bool Activo { get; set; } = true;
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
}

public static class TiposAnuncio
{
    public const string Informativo = "Informativo";
    public const string Importante = "Importante";
}

/// <summary>Actividad inusual que el equipo administrador debe revisar (intentos fallidos, bloqueos de cuenta).</summary>
public class AlertaSeguridad
{
    public int Id { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public string Detalle { get; set; } = string.Empty;

    /// <summary>La cuenta afectada.</summary>
    public int? UsuarioId { get; set; }
    public Usuario? Usuario { get; set; }

    public DateTime Fecha { get; set; } = DateTime.UtcNow;
    public bool Revisada { get; set; }
    public int? RevisadaPorUsuarioId { get; set; }
    public DateTime? FechaRevision { get; set; }
}

public static class TiposAlerta
{
    public const string IntentosFallidos = "IntentosFallidos";
    public const string CuentaBloqueada = "CuentaBloqueada";
}
