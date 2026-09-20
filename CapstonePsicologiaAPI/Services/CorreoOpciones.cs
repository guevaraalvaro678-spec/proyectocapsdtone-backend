namespace CapstonePsicologiaAPI.Services;

/// <summary>Configuración del envío de correos (sección "Correo" de appsettings.json).</summary>
public class CorreoOpciones
{
    public string Host { get; set; } = string.Empty;
    public int Puerto { get; set; } = 587;
    public string Usuario { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Remitente { get; set; } = string.Empty;
    public string NombreRemitente { get; set; } = "Capstone Psicología";

    /// <summary>None, StartTls (puerto 587) o Ssl (puerto 465).</summary>
    public string Seguridad { get; set; } = "StartTls";

    /// <summary>Dirección del front, para los enlaces de los correos.</summary>
    public string FrontUrl { get; set; } = "http://localhost:4200";

    /// <summary>Sin servidor y remitente el sistema no envía nada: los correos quedan pendientes.</summary>
    public bool Configurado => !string.IsNullOrWhiteSpace(Host) && !string.IsNullOrWhiteSpace(Remitente);
}
