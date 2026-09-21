namespace CapstonePsicologiaAPI.Models;

/// <summary>
/// Perfil de un voluntario. La clave primaria es el DNI; la cuenta de acceso vive en Usuarios (`UsuarioId`).
/// </summary>
public class Voluntario
{
    /// <summary>DNI de 8 dígitos: clave primaria.</summary>
    public string Dni { get; set; } = string.Empty;

    public int UsuarioId { get; set; }
    public Usuario? Usuario { get; set; }

    public string Apellidos { get; set; } = string.Empty;
    public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;
}
