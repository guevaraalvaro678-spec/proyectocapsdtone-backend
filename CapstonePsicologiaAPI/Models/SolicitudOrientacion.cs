namespace CapstonePsicologiaAPI.Models;

/// <summary>Solicitud de orientación psicológica que hace un usuario y gestiona un administrador.</summary>
public class SolicitudOrientacion
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }
    public Usuario? Usuario { get; set; }
    public string Motivo { get; set; } = string.Empty;
    public string Estado { get; set; } = EstadosSolicitud.Pendiente;

    /// <summary>Psicólogo que atenderá la solicitud (se define al pasarla a "Asignada").</summary>
    public int? PsicologoAsignadoId { get; set; }
    public Usuario? PsicologoAsignado { get; set; }

    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public DateTime FechaActualizacion { get; set; } = DateTime.UtcNow;
}

public static class EstadosSolicitud
{
    public const string Pendiente = "Pendiente";
    public const string EnRevision = "EnRevision";
    public const string Asignada = "Asignada";
    public const string Cerrada = "Cerrada";
}
