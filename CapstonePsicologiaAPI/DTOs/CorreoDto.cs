namespace CapstonePsicologiaAPI.DTOs;

/// <summary>Un correo de la bandeja de salida, tal como lo ve el administrador.</summary>
public class CorreoDto
{
    public int Id { get; set; }
    public string Destinatario { get; set; } = string.Empty;
    public string? NombreDestinatario { get; set; }
    public string Asunto { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public string? Error { get; set; }
    public string? Lote { get; set; }
    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaEnvio { get; set; }
}
