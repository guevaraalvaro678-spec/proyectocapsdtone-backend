using CapstonePsicologiaAPI.DTOs;

namespace CapstonePsicologiaAPI.Services;

public interface IAdminService
{
    /// <summary>Devuelve la sesión (con token) si las credenciales son válidas; null si no.</summary>
    Task<AdminSesionDto?> LoginAsync(AdminLoginDto login);

    /// <summary>Login con Google: la cuenta debe existir (se crea desde "Únete"). Lanza 404 si no hay cuenta con ese correo.</summary>
    Task<AdminSesionDto> LoginGoogleAsync(string credential);

    /// <summary>Crea un administrador nuevo; devuelve null si el nombre de usuario ya existe.</summary>
    Task<AdminDto?> RegistrarAsync(AdminRegistroDto registro);
}
