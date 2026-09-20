using System.ComponentModel.DataAnnotations;

namespace CapstonePsicologiaAPI.DTOs;

/// <summary>Fila de la tabla de psicólogos del panel de administración.</summary>
public class PsicologoListaDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Correo { get; set; }
    public string? Telefono { get; set; }
    public string? Especialidad { get; set; }
    public string? NumeroColegiatura { get; set; }
    public bool Activo { get; set; }
    public DateTime? UltimoAcceso { get; set; }

    /// <summary>Citas programadas o confirmadas que aún no ocurren.</summary>
    public int CitasProximas { get; set; }

    /// <summary>Solicitudes de orientación que tiene asignadas y siguen abiertas.</summary>
    public int SolicitudesAsignadas { get; set; }
}

/// <summary>Detalle de un psicólogo: perfil, próximas citas y solicitudes asignadas.</summary>
public class PsicologoDetalleDto : PsicologoListaDto
{
    public DateTime FechaRegistro { get; set; }
    public List<CitaDto> Citas { get; set; } = new();
    public List<SolicitudDto> Solicitudes { get; set; } = new();

    /// <summary>Bloques semanales de horario que el psicólogo dejó disponibles.</summary>
    public List<DisponibilidadDto> Disponibilidad { get; set; } = new();
}

public class PsicologoCrearDto
{
    [Required, StringLength(100, MinimumLength = 3)]
    public string Nombre { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(150)]
    public string Correo { get; set; } = string.Empty;

    /// <summary>Contraseña inicial; el psicólogo puede cambiarla con "olvidé mi contraseña".</summary>
    [Required, MinLength(6), MaxLength(100)]
    public string Password { get; set; } = string.Empty;

    [StringLength(30)]
    public string? Telefono { get; set; }

    [Required, StringLength(100, MinimumLength = 3)]
    public string Especialidad { get; set; } = string.Empty;

    [StringLength(30)]
    public string? NumeroColegiatura { get; set; }
}

public class PsicologoActualizarDto
{
    [Required, StringLength(100, MinimumLength = 3)]
    public string Nombre { get; set; } = string.Empty;

    [StringLength(30)]
    public string? Telefono { get; set; }

    [Required, StringLength(100, MinimumLength = 3)]
    public string Especialidad { get; set; } = string.Empty;

    [StringLength(30)]
    public string? NumeroColegiatura { get; set; }
}
