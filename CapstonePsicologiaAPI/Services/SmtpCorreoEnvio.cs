using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace CapstonePsicologiaAPI.Services;

/// <summary>Envía los correos por SMTP con MailKit. Funciona con cualquier proveedor (Gmail, Outlook, Brevo...).</summary>
public class SmtpCorreoEnvio : ICorreoEnvio
{
    private readonly CorreoOpciones _opciones;

    public SmtpCorreoEnvio(IOptions<CorreoOpciones> opciones)
    {
        _opciones = opciones.Value;
    }

    public bool Configurado => _opciones.Configurado;

    public async Task EnviarAsync(string destinatario, string? nombre, string asunto, string html, CancellationToken ct)
    {
        var mensaje = new MimeMessage();
        mensaje.From.Add(new MailboxAddress(_opciones.NombreRemitente, _opciones.Remitente));
        mensaje.To.Add(new MailboxAddress(nombre ?? destinatario, destinatario));
        mensaje.Subject = asunto;
        mensaje.Body = new BodyBuilder { HtmlBody = html }.ToMessageBody();

        var seguridad = _opciones.Seguridad switch
        {
            "Ssl" => SecureSocketOptions.SslOnConnect,
            "None" => SecureSocketOptions.None,
            _ => SecureSocketOptions.StartTls,
        };

        using var smtp = new SmtpClient();
        await smtp.ConnectAsync(_opciones.Host, _opciones.Puerto, seguridad, ct);

        if (!string.IsNullOrWhiteSpace(_opciones.Usuario))
            await smtp.AuthenticateAsync(_opciones.Usuario, _opciones.Password, ct);

        await smtp.SendAsync(mensaje, ct);
        await smtp.DisconnectAsync(true, ct);
    }
}
