using CapstonePsicologiaAPI.DTOs;

namespace CapstonePsicologiaAPI.Services;

public interface IAdminService
{
    /// <summary>
    /// Valida usuario y contraseña (null si no son válidos). Con verificación en dos pasos devuelve un desafío
    /// y los canales para recibir el código; sin ella, la sesión directamente.
    /// </summary>
    Task<LoginResultadoDto?> LoginAsync(AdminLoginDto login);

    /// <summary>Envía el código de 6 dígitos por el canal elegido (correo o SMS).</summary>
    Task<CodigoEnviadoDto> SolicitarCodigoAsync(CodigoSolicitudDto datos);

    /// <summary>Comprueba el código y, si es correcto, entrega la sesión.</summary>
    Task<AdminSesionDto> VerificarCodigoAsync(CodigoVerificarDto datos);

    /// <summary>Login con Google: la cuenta debe existir (se crea desde "Únete"). Lanza 404 si no hay cuenta con ese correo.</summary>
    Task<AdminSesionDto> LoginGoogleAsync(string credential);

    /// <summary>Crea un administrador nuevo; devuelve null si el nombre de usuario ya existe.</summary>
    Task<AdminDto?> RegistrarAsync(AdminRegistroDto registro);
}
