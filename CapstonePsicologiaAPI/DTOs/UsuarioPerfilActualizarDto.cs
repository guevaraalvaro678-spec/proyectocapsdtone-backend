using System.ComponentModel.DataAnnotations;

namespace CapstonePsicologiaAPI.DTOs;

/// <summary>Datos del perfil y preferencias que el usuario puede modificar.</summary>
public class UsuarioPerfilActualizarDto
{
    [Required, StringLength(100, MinimumLength = 3)]
    public string Nombre { get; set; } = string.Empty;

    [StringLength(100)]
    public string? Ubicacion { get; set; }

    [StringLength(200)]
    public string? Intereses { get; set; }

    public bool RecordatoriosCorreo { get; set; }
    public bool PreferenciaVirtual { get; set; }
}
