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

    public AdminService(IAdminData adminData, IPasswordHasher<Usuario> hasher, ITokenService tokenService)
    {
        _adminData = adminData;
        _hasher = hasher;
        _tokenService = tokenService;
    }

    public async Task<AdminSesionDto?> LoginAsync(AdminLoginDto login)
    {
        var nombre = Normalizar(login.Usuario);
        var usuario = await _adminData.ObtenerPorNombreUsuarioAsync(nombre);

        if (usuario is null ||
            _hasher.VerifyHashedPassword(usuario, usuario.PasswordHash, login.Password) == PasswordVerificationResult.Failed)
        {
            return null;
        }

        return new AdminSesionDto
        {
            Id = usuario.Id,
            Usuario = usuario.NombreUsuario,
            Rol = usuario.Rol,
            Token = _tokenService.Generar(usuario),
        };
    }

    public async Task<AdminDto?> RegistrarAsync(AdminRegistroDto registro)
    {
        var nombre = Normalizar(registro.Usuario);

        if (await _adminData.ExisteNombreUsuarioAsync(nombre))
            return null;

        var usuario = new Usuario { NombreUsuario = nombre, Rol = registro.Rol };
        usuario.PasswordHash = _hasher.HashPassword(usuario, registro.Password);
        await _adminData.CrearAsync(usuario);

        return new AdminDto { Id = usuario.Id, Usuario = usuario.NombreUsuario, Rol = usuario.Rol };
    }

    private static string Normalizar(string nombreUsuario) => nombreUsuario.Trim().ToLowerInvariant();
}
