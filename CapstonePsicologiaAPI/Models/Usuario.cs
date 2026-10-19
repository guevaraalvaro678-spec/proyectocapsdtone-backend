namespace CapstonePsicologiaAPI.Models;

/// <summary>
/// Cuenta del sistema: administradores (SuperAdmin, Admin), psicólogos (Psicologo)
/// y usuarios comunes (Usuario).
/// </summary>
public class Usuario
{
    public int Id { get; set; }

    /// <summary>Nombre para iniciar sesión. En usuarios comunes y psicólogos es su correo.</summary>
    public string NombreUsuario { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;
    public string Rol { get; set; } = Roles.Admin;

    /// <summary>Una cuenta desactivada no puede iniciar sesión, pero conserva su historial.</summary>
    public bool Activo { get; set; } = true;

    // Perfil
    public string Nombre { get; set; } = string.Empty;
    public string? Correo { get; set; }
    public string? Telefono { get; set; }
    public string? Ubicacion { get; set; }
    public string? Intereses { get; set; }

    // Solo psicólogos
    public string? Especialidad { get; set; }
    public string? NumeroColegiatura { get; set; }

    // Preferencias
    public bool RecordatoriosCorreo { get; set; } = true;
    public bool PreferenciaVirtual { get; set; }

    public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;
    public DateTime? UltimoAcceso { get; set; }

    // Bloqueo temporal tras varios intentos fallidos de contraseña.
    public int IntentosFallidos { get; set; }
    public DateTime? BloqueadoHasta { get; set; }

    // Recuperación de contraseña: solo se guarda el hash del token, nunca el token.
    public string? TokenRecuperacionHash { get; set; }
    public DateTime? TokenRecuperacionExpira { get; set; }
}
