namespace CapstonePsicologiaAPI.DTOs;

public class SolicitudDto
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }
    public string UsuarioNombre { get; set; } = string.Empty;
    public string? UsuarioCorreo { get; set; }
    public string Motivo { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public int? PsicologoAsignadoId { get; set; }
    public string? PsicologoAsignadoNombre { get; set; }
    public DateTime FechaCreacion { get; set; }
    public DateTime FechaActualizacion { get; set; }
}
