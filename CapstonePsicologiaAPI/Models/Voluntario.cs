namespace CapstonePsicologiaAPI.Models;

/// <summary>
/// Perfil de un voluntario. La clave primaria es su cuenta (`UsuarioId`): al postular como voluntario
/// no se pide DNI, así que puede quedar vacío.
/// </summary>
public class Voluntario
{
    /// <summary>DNI de 8 dígitos (opcional; único cuando existe).</summary>
    public string? Dni { get; set; }

    public int UsuarioId { get; set; }
    public Usuario? Usuario { get; set; }

    public string Apellidos { get; set; } = string.Empty;
    public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;
}
