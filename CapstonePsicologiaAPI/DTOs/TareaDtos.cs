using System.ComponentModel.DataAnnotations;

namespace CapstonePsicologiaAPI.DTOs;

public class TareaCrearDto
{
    [Range(1, int.MaxValue)]
    public int UsuarioId { get; set; }

    [Required, StringLength(150, MinimumLength = 3)]
    public string Titulo { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Descripcion { get; set; }

    public DateTime? FechaLimite { get; set; }
    public bool Urgente { get; set; }
}

public class TareaEstadoDto
{
    [Required, RegularExpression("^(Pendiente|Completada)$", ErrorMessage = "Estado no válido")]
    public string Estado { get; set; } = string.Empty;
}

public class TareaDto
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }
    public string UsuarioNombre { get; set; } = string.Empty;
    public string UsuarioRol { get; set; } = string.Empty;
    public string Titulo { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public DateTime? FechaLimite { get; set; }
    public bool Urgente { get; set; }
    public string Estado { get; set; } = string.Empty;
    public DateTime FechaCreacion { get; set; }
}

public class MarcacionDto
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }
    public string UsuarioNombre { get; set; } = string.Empty;
    public DateTime Entrada { get; set; }
    public DateTime? Salida { get; set; }
}
