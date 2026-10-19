using System.Net.Http.Headers;
using System.Text;

namespace CapstonePsicologiaAPI.Services;

/// <summary>Envío de SMS (códigos de verificación). Solo está disponible si hay un proveedor configurado.</summary>
public interface ISmsService
{
    bool Disponible { get; }

    /// <summary>Convierte un teléfono peruano al formato internacional (+51...). Null si no es válido.</summary>
    string? Normalizar(string? telefono);

    Task EnviarAsync(string telefonoInternacional, string mensaje);
}

/// <summary>
/// SMS con Twilio (servicio de pago). Se activa llenando Sms:Twilio:AccountSid, AuthToken y Numero
/// en appsettings.Local.json; sin eso el canal SMS no se ofrece en el login.
/// </summary>
public class TwilioSmsService : ISmsService
{
    private readonly HttpClient _http;
    private readonly string? _cuenta;
    private readonly string? _token;
    private readonly string? _numero;

    public TwilioSmsService(HttpClient http, IConfiguration config)
    {
        _http = http;
        _cuenta = config["Sms:Twilio:AccountSid"];
        _token = config["Sms:Twilio:AuthToken"];
        _numero = config["Sms:Twilio:Numero"];
    }

    public bool Disponible =>
        !string.IsNullOrWhiteSpace(_cuenta) && !string.IsNullOrWhiteSpace(_token) && !string.IsNullOrWhiteSpace(_numero);

    public string? Normalizar(string? telefono)
    {
        if (string.IsNullOrWhiteSpace(telefono)) return null;
        var digitos = new string(telefono.Where(char.IsDigit).ToArray());
        if (digitos.Length == 9 && digitos[0] == '9') return "+51" + digitos;
        if (digitos.Length == 11 && digitos.StartsWith("519")) return "+" + digitos;
        return null;
    }

    public async Task EnviarAsync(string telefonoInternacional, string mensaje)
    {
        if (!Disponible)
            throw ReglaNegocioException.Invalida("El envío por SMS no está configurado");

        using var peticion = new HttpRequestMessage(HttpMethod.Post, $"https://api.twilio.com/2010-04-01/Accounts/{_cuenta}/Messages.json")
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["To"] = telefonoInternacional,
                ["From"] = _numero!,
                ["Body"] = mensaje,
            }),
        };
        peticion.Headers.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes($"{_cuenta}:{_token}")));

        var respuesta = await _http.SendAsync(peticion);
        if (!respuesta.IsSuccessStatusCode)
            throw ReglaNegocioException.Invalida("No se pudo enviar el SMS. Intenta con el correo.");
    }
}
