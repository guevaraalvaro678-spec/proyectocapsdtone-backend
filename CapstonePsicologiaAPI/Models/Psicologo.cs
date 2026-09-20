namespace CapstonePsicologiaAPI.Models;

/// <summary>
/// Perfil profesional de un psicólogo colaborador y estado de su solicitud. La persona postula desde el sitio
/// público y queda Pendiente, sin cuenta de acceso; al aprobarla el administrador se crea su cuenta en
/// Usuarios (rol Psicologo) y se enlaza en `UsuarioId`.
/// </summary>
public class Psicologo
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Correo { get; set; } = string.Empty;

    /// <summary>Hash de la contraseña elegida al postular. Se pasa a la cuenta al aprobar y se borra de aquí.</summary>
    public string? PasswordHash { get; set; }

    public string? Telefono { get; set; }
    public string Especialidad { get; set; } = string.Empty;
    public string? NumeroColegiatura { get; set; }

    /// <summary>Presentación profesional breve.</summary>
    public string? Presentacion { get; set; }

    public string Estado { get; set; } = EstadosColaborador.Pendiente;
    public string? MotivoRechazo { get; set; }
    public DateTime FechaSolicitud { get; set; } = DateTime.UtcNow;
    public DateTime? FechaRevision { get; set; }
    public int? RevisadoPorUsuarioId { get; set; }

    /// <summary>Cuenta de acceso; solo existe cuando la solicitud fue aprobada.</summary>
    public int? UsuarioId { get; set; }
    public Usuario? Usuario { get; set; }
}

public static class EstadosColaborador
{
    public const string Pendiente = "Pendiente";
    public const string Aprobado = "Aprobado";
    public const string Rechazado = "Rechazado";
}
