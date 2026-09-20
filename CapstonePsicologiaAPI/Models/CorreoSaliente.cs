namespace CapstonePsicologiaAPI.Models;

/// <summary>
/// Correo que el sistema debe enviar. Se guarda siempre en la base (bandeja de salida) y un
/// proceso en segundo plano lo envía por SMTP cuando el servidor está configurado.
/// </summary>
public class CorreoSaliente
{
    public int Id { get; set; }
    public string Destinatario { get; set; } = string.Empty;
    public string? NombreDestinatario { get; set; }
    public string Asunto { get; set; } = string.Empty;

    /// <summary>Contenido en HTML.</summary>
    public string Cuerpo { get; set; } = string.Empty;

    public string Tipo { get; set; } = TiposCorreo.Masivo;
    public string Estado { get; set; } = EstadosCorreo.Pendiente;
    public string? Error { get; set; }

    /// <summary>Agrupa los correos de un mismo envío masivo.</summary>
    public string? Lote { get; set; }

    public int? CreadoPorUsuarioId { get; set; }
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public DateTime? FechaEnvio { get; set; }
}

public static class EstadosCorreo
{
    public const string Pendiente = "Pendiente";
    public const string Enviando = "Enviando";
    public const string Enviado = "Enviado";
    public const string Fallido = "Fallido";
}

public static class TiposCorreo
{
    public const string Bienvenida = "Bienvenida";
    public const string EstadoSolicitud = "EstadoSolicitud";
    public const string RecuperarPassword = "RecuperarPassword";
    public const string NuevaCita = "NuevaCita";
    public const string RecordatorioCita = "RecordatorioCita";
    public const string CitaCancelada = "CitaCancelada";
    public const string RespuestaContacto = "RespuestaContacto";
    public const string Masivo = "Masivo";
}
