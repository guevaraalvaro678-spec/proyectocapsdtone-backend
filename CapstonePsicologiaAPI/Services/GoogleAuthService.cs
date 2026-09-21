using CapstonePsicologiaAPI.DTOs;
using Google.Apis.Auth;
using Microsoft.Extensions.Options;

namespace CapstonePsicologiaAPI.Services;

/// <summary>Configuración de "Iniciar con Google" (sección "Google" de appsettings). Sin ClientId la función queda apagada.</summary>
public class GoogleOpciones
{
    public string ClientId { get; set; } = string.Empty;
    public bool Configurado => !string.IsNullOrWhiteSpace(ClientId);
}

public interface IGoogleAuthService
{
    bool Configurado { get; }
    string? ClientId { get; }

    /// <summary>Valida con Google el token que entregó el botón y devuelve los datos de la cuenta.</summary>
    Task<GooglePerfilDto> ValidarAsync(string credential);
}

public class GoogleAuthService : IGoogleAuthService
{
    private readonly GoogleOpciones _opciones;

    public GoogleAuthService(IOptions<GoogleOpciones> opciones)
    {
        _opciones = opciones.Value;
    }

    public bool Configurado => _opciones.Configurado;
    public string? ClientId => Configurado ? _opciones.ClientId : null;

    public async Task<GooglePerfilDto> ValidarAsync(string credential)
    {
        if (!Configurado)
            throw ReglaNegocioException.Invalida("El acceso con Google todavía no está configurado");

        try
        {
            var datos = await GoogleJsonWebSignature.ValidateAsync(
                credential,
                new GoogleJsonWebSignature.ValidationSettings { Audience = new[] { _opciones.ClientId } });

            if (!datos.EmailVerified || string.IsNullOrWhiteSpace(datos.Email))
                throw ReglaNegocioException.Invalida("Tu cuenta de Google no tiene el correo verificado");

            return new GooglePerfilDto
            {
                Correo = datos.Email.Trim().ToLowerInvariant(),
                Nombres = datos.GivenName ?? datos.Name ?? string.Empty,
                Apellidos = datos.FamilyName ?? string.Empty,
            };
        }
        catch (InvalidJwtException)
        {
            throw ReglaNegocioException.Invalida("No se pudo validar tu cuenta de Google. Inténtalo de nuevo.");
        }
    }
}
