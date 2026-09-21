namespace CapstonePsicologiaAPI.DTOs;

/// <summary>Perfil del usuario que se muestra en "Mi espacio".</summary>
public class UsuarioPerfilDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Correo { get; set; }

    /// <summary>Datos del perfil de voluntario; nulos en cuentas creadas antes de existir esa tabla.</summary>
    public string? Dni { get; set; }
    public string? Universidad { get; set; }
    public string? Carrera { get; set; }
    public string? Ciclo { get; set; }
    public string? Ubicacion { get; set; }
    public string? Intereses { get; set; }
    public bool RecordatoriosCorreo { get; set; }
    public bool PreferenciaVirtual { get; set; }
    public DateTime FechaRegistro { get; set; }
}
