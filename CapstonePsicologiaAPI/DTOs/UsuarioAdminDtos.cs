using System.ComponentModel.DataAnnotations;

namespace CapstonePsicologiaAPI.DTOs;

/// <summary>Fila de la tabla de usuarios del panel de administración.</summary>
public class UsuarioListaDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Correo { get; set; }
    public string NombreUsuario { get; set; } = string.Empty;
    public string? Telefono { get; set; }
    public string Rol { get; set; } = string.Empty;
    public bool Activo { get; set; }
    public DateTime FechaRegistro { get; set; }
    public DateTime? UltimoAcceso { get; set; }
}

/// <summary>Detalle de un usuario para el administrador: perfil, solicitudes y citas.</summary>
public class UsuarioDetalleDto : UsuarioListaDto
{
    public string? Ubicacion { get; set; }
    public string? Intereses { get; set; }
    public bool RecordatoriosCorreo { get; set; }
    public bool PreferenciaVirtual { get; set; }
    public List<SolicitudDto> Solicitudes { get; set; } = new();
    public List<CitaDto> Citas { get; set; } = new();
}

public class UsuarioActivoDto
{
    public bool Activo { get; set; }
}

public class RecuperarPasswordDto
{
    [Required, EmailAddress, StringLength(150)]
    public string Correo { get; set; } = string.Empty;
}

public class RestablecerPasswordDto
{
    [Required, EmailAddress, StringLength(150)]
    public string Correo { get; set; } = string.Empty;

    [Required, StringLength(200)]
    public string Token { get; set; } = string.Empty;

    [Required, MinLength(8), MaxLength(100)]
    public string NuevaPassword { get; set; } = string.Empty;
}
