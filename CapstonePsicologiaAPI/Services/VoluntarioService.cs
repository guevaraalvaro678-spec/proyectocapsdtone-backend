using System.Security.Cryptography;
using CapstonePsicologiaAPI.Data;
using CapstonePsicologiaAPI.DTOs;
using CapstonePsicologiaAPI.Models;
using Microsoft.AspNetCore.Identity;

namespace CapstonePsicologiaAPI.Services;

public interface IVoluntarioService
{
    /// <summary>Devuelve la sesión solo si se registró con Google (entra directo); si no, null: recibe su clave por correo.</summary>
    Task<AdminSesionDto?> RegistrarAsync(VoluntarioRegistroDto datos);
    Task<VoluntarioDto> ObtenerYoAsync(int usuarioId);
    Task<List<VoluntarioDto>> ListarAsync(string? buscar);
}

/// <summary>
/// Voluntarios: se registran con lo mínimo (sin contraseña). La cuenta se crea al instante con una contraseña
/// temporal que llega por correo; pueden cambiarla desde "¿Olvidaste tu contraseña?".
/// </summary>
public class VoluntarioService : IVoluntarioService
{
    private const string Alfabeto = "abcdefghjkmnpqrstuvwxyzABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    private readonly IVoluntarioData _voluntarioData;
    private readonly IUsuarioData _usuarioData;
    private readonly IPasswordHasher<Usuario> _hasher;
    private readonly ICorreoService _correo;
    private readonly IGoogleAuthService _google;
    private readonly ITokenService _tokenService;

    public VoluntarioService(IVoluntarioData voluntarioData, IUsuarioData usuarioData, IPasswordHasher<Usuario> hasher, ICorreoService correo, IGoogleAuthService google, ITokenService tokenService)
    {
        _voluntarioData = voluntarioData;
        _usuarioData = usuarioData;
        _hasher = hasher;
        _correo = correo;
        _google = google;
        _tokenService = tokenService;
    }

    public async Task<AdminSesionDto?> RegistrarAsync(VoluntarioRegistroDto datos)
    {
        var correo = datos.Correo.Trim().ToLowerInvariant();

        if (await _usuarioData.ExisteCorreoAsync(correo))
            throw ReglaNegocioException.Conflicto("Ese correo ya está registrado");

        if (await _voluntarioData.ExisteDniAsync(datos.Dni))
            throw ReglaNegocioException.Conflicto("Ese DNI ya está registrado");

        // Con Google el correo ya viene verificado y entra directo, sin contraseña temporal por correo.
        var conGoogle = !string.IsNullOrWhiteSpace(datos.GoogleCredential);
        if (conGoogle && (await _google.ValidarAsync(datos.GoogleCredential!)).Correo != correo)
            throw ReglaNegocioException.Invalida("El correo debe ser el de tu cuenta de Google");

        var clave = ClaveTemporal();
        var usuario = new Usuario
        {
            NombreUsuario = correo,
            Correo = correo,
            Nombre = datos.Nombres.Trim(),
            Telefono = datos.Telefono.Trim(),
            Rol = Roles.Voluntario,
            FechaRegistro = DateTime.UtcNow,
        };
        usuario.PasswordHash = _hasher.HashPassword(usuario, clave);
        await _usuarioData.CrearAsync(usuario);

        await _voluntarioData.CrearAsync(new Voluntario
        {
            Dni = datos.Dni,
            UsuarioId = usuario.Id,
            Apellidos = datos.Apellidos.Trim(),
        });

        if (conGoogle)
        {
            await _correo.EncolarAsync(correo, usuario.Nombre, "¡Bienvenido/a a nuestro voluntariado!",
                PlantillasCorreo.Bienvenida(usuario.Nombre, $"{_correo.FrontUrl}/voluntario"), TiposCorreo.Bienvenida);
            return AdminService.ASesion(usuario, _tokenService);
        }

        await _correo.EncolarAsync(correo, usuario.Nombre, "Ya eres voluntario/a: tu acceso",
            PlantillasCorreo.BienvenidaVoluntario(usuario.Nombre, correo, clave, $"{_correo.FrontUrl}/login"),
            TiposCorreo.Bienvenida);
        return null;
    }

    public async Task<VoluntarioDto> ObtenerYoAsync(int usuarioId)
    {
        var voluntario = await _voluntarioData.ObtenerPorUsuarioIdAsync(usuarioId);

        if (voluntario is not null)
            return ADto(voluntario);

        // Cuentas anteriores a la tabla Voluntarios: tienen acceso pero no DNI ni apellidos.
        var usuario = await _usuarioData.ObtenerPorIdAsync(usuarioId)
                      ?? throw ReglaNegocioException.NoEncontrada("Cuenta no encontrada");

        return new VoluntarioDto
        {
            UsuarioId = usuario.Id,
            Nombre = usuario.Nombre,
            Correo = usuario.Correo,
            Telefono = usuario.Telefono,
            Activo = usuario.Activo,
            FechaRegistro = Mapeos.Utc(usuario.FechaRegistro),
        };
    }

    public async Task<List<VoluntarioDto>> ListarAsync(string? buscar) =>
        (await _voluntarioData.ListarAsync(buscar)).Select(ADto).ToList();

    private static VoluntarioDto ADto(Voluntario v) => new()
    {
        UsuarioId = v.UsuarioId,
        Dni = v.Dni ?? string.Empty,
        Nombre = v.Usuario!.Nombre,
        Apellidos = v.Apellidos,
        Correo = v.Usuario.Correo,
        Telefono = v.Usuario.Telefono,
        Activo = v.Usuario.Activo,
        FechaRegistro = Mapeos.Utc(v.FechaRegistro),
    };

    private static string ClaveTemporal() =>
        string.Create(10, 0, (span, _) =>
        {
            for (var i = 0; i < span.Length; i++)
                span[i] = Alfabeto[RandomNumberGenerator.GetInt32(Alfabeto.Length)];
        });
}
