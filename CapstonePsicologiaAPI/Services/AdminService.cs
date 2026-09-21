using CapstonePsicologiaAPI.Data;
using CapstonePsicologiaAPI.DTOs;
using CapstonePsicologiaAPI.Models;
using Microsoft.AspNetCore.Identity;

namespace CapstonePsicologiaAPI.Services;

public class AdminService : IAdminService
{
    private readonly IAdminData _adminData;
    private readonly IPasswordHasher<Usuario> _hasher;
    private readonly ITokenService _tokenService;
    private readonly IGoogleAuthService _google;

    public AdminService(IAdminData adminData, IPasswordHasher<Usuario> hasher, ITokenService tokenService, IGoogleAuthService google)
    {
        _adminData = adminData;
        _hasher = hasher;
        _tokenService = tokenService;
        _google = google;
    }

    /// <summary>Login de cualquier cuenta: administradores (por usuario), usuarios y psicólogos (por correo).</summary>
    public async Task<AdminSesionDto?> LoginAsync(AdminLoginDto login)
    {
        var nombre = Normalizar(login.Usuario);
        var usuario = await _adminData.ObtenerPorUsuarioOCorreoAsync(nombre);

        if (usuario is null ||
            _hasher.VerifyHashedPassword(usuario, usuario.PasswordHash, login.Password) == PasswordVerificationResult.Failed)
        {
            return null;
        }

        // Solo se avisa que la cuenta está desactivada a quien ya demostró conocer la contraseña.
        if (!usuario.Activo)
            throw ReglaNegocioException.Prohibida("Tu cuenta está desactivada. Contacta al administrador.");

        await _adminData.RegistrarAccesoAsync(usuario.Id);

        return ASesion(usuario, _tokenService);
    }

    public async Task<AdminSesionDto> LoginGoogleAsync(string credential)
    {
        var perfil = await _google.ValidarAsync(credential);
        var usuario = await _adminData.ObtenerPorUsuarioOCorreoAsync(perfil.Correo)
                      ?? throw ReglaNegocioException.NoEncontrada("No hay una cuenta con ese correo. Regístrate primero en \"Únete\".");

        if (!usuario.Activo)
            throw ReglaNegocioException.Prohibida("Tu cuenta está desactivada. Contacta al administrador.");

        await _adminData.RegistrarAccesoAsync(usuario.Id);
        return ASesion(usuario, _tokenService);
    }

    public async Task<AdminDto?> RegistrarAsync(AdminRegistroDto registro)
    {
        var nombre = Normalizar(registro.Usuario);

        if (await _adminData.ExisteNombreUsuarioAsync(nombre))
            return null;

        var usuario = new Usuario { NombreUsuario = nombre, Nombre = registro.Usuario.Trim(), Rol = registro.Rol };
        usuario.PasswordHash = _hasher.HashPassword(usuario, registro.Password);
        await _adminData.CrearAsync(usuario);

        return new AdminDto { Id = usuario.Id, Usuario = usuario.NombreUsuario, Rol = usuario.Rol };
    }

    /// <summary>Arma la respuesta de sesión (con token) para una cuenta. La comparten login y registro de usuarios.</summary>
    public static AdminSesionDto ASesion(Usuario usuario, ITokenService tokenService) => new()
    {
        Id = usuario.Id,
        Usuario = usuario.NombreUsuario,
        Nombre = string.IsNullOrWhiteSpace(usuario.Nombre) ? usuario.NombreUsuario : usuario.Nombre,
        Rol = usuario.Rol,
        Token = tokenService.Generar(usuario),
    };

    private static string Normalizar(string valor) => valor.Trim().ToLowerInvariant();
}
