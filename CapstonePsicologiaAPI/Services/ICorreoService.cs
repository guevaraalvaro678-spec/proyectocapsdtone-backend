using CapstonePsicologiaAPI.DTOs;

namespace CapstonePsicologiaAPI.Services;

public interface ICorreoService
{
    /// <summary>True si el servidor tiene SMTP configurado y por lo tanto los correos realmente salen.</summary>
    bool SmtpConfigurado { get; }

    /// <summary>Dirección del front, sin barra final, para armar enlaces.</summary>
    string FrontUrl { get; }

    /// <summary>
    /// Deja un correo en la bandeja de salida. Por defecto no lanza errores: un correo automático nunca
    /// debe hacer fallar la operación que lo originó (registrarse, agendar una cita...). Con
    /// `propagarErrores` sí lo hace, para acciones cuyo único fin es enviar el correo (responder un mensaje).
    /// </summary>
    Task EncolarAsync(string destinatario, string? nombre, string asunto, string cuerpoHtml, string tipo, int? creadoPor = null, bool propagarErrores = false);

    /// <summary>Correo o notificación redactado por un administrador. Devuelve null si no hay destinatarios.</summary>
    Task<CorreoEnvioResultadoDto?> EnviarAsync(CorreoEnviarDto datos, int administradorId);

    Task<List<CorreoDto>> HistorialAsync();
    Task<CorreoEstadoDto> ObtenerEstadoAsync();

    /// <summary>Devuelve false si el correo no existe o no estaba fallido.</summary>
    Task<bool> ReintentarAsync(int id);

    /// <summary>Envía los correos pendientes. No hace nada si el SMTP no está configurado.</summary>
    Task ProcesarPendientesAsync(CancellationToken ct);
}
