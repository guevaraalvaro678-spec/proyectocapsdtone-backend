namespace CapstonePsicologiaAPI.Models;

/// <summary>Actividad que el administrador o Recursos Humanos deja a un voluntario o practicante.</summary>
public class Tarea
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }
    public Usuario? Usuario { get; set; }

    public string Titulo { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public DateTime? FechaLimite { get; set; }

    /// <summary>Marca las tareas de emergencia.</summary>
    public bool Urgente { get; set; }

    public string Estado { get; set; } = EstadosTarea.Pendiente;
    public int CreadaPorUsuarioId { get; set; }
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
}

public static class EstadosTarea
{
    public const string Pendiente = "Pendiente";
    public const string Completada = "Completada";
}
