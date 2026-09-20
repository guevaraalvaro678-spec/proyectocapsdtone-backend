namespace CapstonePsicologiaAPI.Services;

/// <summary>Transporte de correo: entrega un mensaje ya armado al servidor SMTP.</summary>
public interface ICorreoEnvio
{
    bool Configurado { get; }

    Task EnviarAsync(string destinatario, string? nombre, string asunto, string html, CancellationToken ct);
}
