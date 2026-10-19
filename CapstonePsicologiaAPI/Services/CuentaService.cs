using CapstonePsicologiaAPI.Data;
using CapstonePsicologiaAPI.DTOs;
using CapstonePsicologiaAPI.Models;
using Microsoft.AspNetCore.Identity;

namespace CapstonePsicologiaAPI.Services;

public interface ICuentaService
{
    Task<AdminSesionDto> RegistrarAsync(CuentaRegistroDto datos);

    /// <summary>Entra con Google; si el correo no tiene cuenta, la crea como Postulante.</summary>
    Task<AdminSesionDto> EntrarConGoogleAsync(string credential, bool crearSiNoExiste);

    /// <summary>Nuevo token con el rol actual (por ejemplo, cuando aceptan a un postulante).</summary>
    Task<AdminSesionDto> RenovarSesionAsync(int usuarioId);

    /// <summary>
    /// Cuenta para el envío del formulario de postulación: con Google usa la cuenta de ese correo o la crea;
    /// con correo y contraseña la crea (si el correo ya existe, hay que iniciar sesión).
    /// </summary>
    Task<Usuario> ObtenerOCrearPostulanteAsync(string? correo, string? password, string? googleCredential);

    Task<CuentaDto> ObtenerAsync(int usuarioId);
    Task<CuentaDto> ActualizarPerfilAsync(int usuarioId, CuentaPerfilDto datos);
    Task CambiarPasswordAsync(int usuarioId, CuentaPasswordDto datos);
}

/// <summary>Cuenta propia: crear cuenta ("Crea tu cuenta"), Google, y "Mi cuenta y seguridad".</summary>
public class CuentaService : ICuentaService
{
    private readonly IUsuarioData _usuarioData;
    private readonly IVoluntarioData _voluntarioData;
    private readonly IPracticanteData _practicanteData;
    private readonly IPostulacionData _postulacionData;
    private readonly IPasswordHasher<Usuario> _hasher;
    private readonly ITokenService _tokenService;
    private readonly IGoogleAuthService _google;
    private readonly ICorreoService _correo;

    public CuentaService(
        IUsuarioData usuarioData,
        IVoluntarioData voluntarioData,
        IPracticanteData practicanteData,
        IPostulacionData postulacionData,
        IPasswordHasher<Usuario> hasher,
        ITokenService tokenService,
        IGoogleAuthService google,
        ICorreoService correo)
    {
        _usuarioData = usuarioData;
        _voluntarioData = voluntarioData;
        _practicanteData = practicanteData;
        _postulacionData = postulacionData;
        _hasher = hasher;
        _tokenService = tokenService;
        _google = google;
        _correo = correo;
    }

    public async Task<AdminSesionDto> RegistrarAsync(CuentaRegistroDto datos)
    {
        var correo = datos.Correo.Trim().ToLowerInvariant();

        if (await _usuarioData.ExisteCorreoAsync(correo))
            throw ReglaNegocioException.Conflicto("Ese correo ya tiene una cuenta. Inicia sesión.");

        var usuario = await CrearPostulanteAsync(correo, NombreDesdeCorreo(correo), datos.Password);
        return AdminService.ASesion(usuario, _tokenService);
    }

    public async Task<AdminSesionDto> EntrarConGoogleAsync(string credential, bool crearSiNoExiste)
    {
        var perfil = await _google.ValidarAsync(credential);
        var usuario = await _usuarioData.ObtenerPorCorreoAsync(perfil.Correo);

        if (usuario is not null)
        {
            if (!usuario.Activo)
                throw ReglaNegocioException.Prohibida("Tu cuenta está desactivada. Contacta al administrador.");

            return AdminService.ASesion(usuario, _tokenService);
        }

        // Sin cuenta: el front primero lleva al formulario y la crea recién al enviar la postulación.
        if (!crearSiNoExiste)
            throw ReglaNegocioException.NoEncontrada("Ese correo todavía no tiene cuenta");

        // Se crea como postulante con una contraseña aleatoria (entra siempre con Google).
        var nombre = string.IsNullOrWhiteSpace(perfil.Nombres) ? NombreDesdeCorreo(perfil.Correo) : $"{perfil.Nombres} {perfil.Apellidos}".Trim();
        usuario = await CrearPostulanteAsync(perfil.Correo, nombre, Guid.NewGuid().ToString("N"));
        return AdminService.ASesion(usuario, _tokenService);
    }

    public async Task<AdminSesionDto> RenovarSesionAsync(int usuarioId) =>
        AdminService.ASesion(await ObtenerUsuarioAsync(usuarioId), _tokenService);

    public async Task<Usuario> ObtenerOCrearPostulanteAsync(string? correo, string? password, string? googleCredential)
    {
        if (!string.IsNullOrWhiteSpace(googleCredential))
        {
            var perfil = await _google.ValidarAsync(googleCredential);
            var existente = await _usuarioData.ObtenerPorCorreoAsync(perfil.Correo);
            if (existente is not null)
            {
                if (!existente.Activo)
                    throw ReglaNegocioException.Prohibida("Tu cuenta está desactivada. Contacta al administrador.");
                if (existente.Rol is not (Roles.Postulante or Roles.Voluntario or Roles.Practicante))
                    throw ReglaNegocioException.Prohibida("Esa cuenta es del equipo de AMAAC: inicia sesión desde el login.");
                return existente;
            }

            var nombre = string.IsNullOrWhiteSpace(perfil.Nombres) ? NombreDesdeCorreo(perfil.Correo) : $"{perfil.Nombres} {perfil.Apellidos}".Trim();
            return await CrearPostulanteAsync(perfil.Correo, nombre, Guid.NewGuid().ToString("N"));
        }

        if (string.IsNullOrWhiteSpace(correo) || string.IsNullOrWhiteSpace(password))
            throw ReglaNegocioException.Invalida("Crea tu cuenta: únete con Google o escribe tu correo y una contraseña.");

        var limpio = correo.Trim().ToLowerInvariant();
        if (await _usuarioData.ExisteCorreoAsync(limpio))
            throw ReglaNegocioException.Conflicto("Ese correo ya tiene una cuenta. Inicia sesión y vuelve a \"Participar\".");

        return await CrearPostulanteAsync(limpio, NombreDesdeCorreo(limpio), password);
    }

    public async Task<CuentaDto> ObtenerAsync(int usuarioId)
    {
        var usuario = await ObtenerUsuarioAsync(usuarioId);
        return new CuentaDto
        {
            Id = usuario.Id,
            Usuario = usuario.NombreUsuario,
            Nombre = usuario.Nombre,
            Correo = usuario.Correo,
            Telefono = usuario.Telefono,
            Rol = usuario.Rol,
            Activo = usuario.Activo,
            Dni = OcultarDni(await BuscarDniAsync(usuario)),
            FechaRegistro = Mapeos.Utc(usuario.FechaRegistro),
        };
    }

    public async Task<CuentaDto> ActualizarPerfilAsync(int usuarioId, CuentaPerfilDto datos)
    {
        var usuario = await ObtenerUsuarioAsync(usuarioId);
        usuario.Nombre = datos.Nombre.Trim();
        usuario.Telefono = string.IsNullOrWhiteSpace(datos.Telefono) ? null : datos.Telefono.Trim();
        await _usuarioData.ActualizarAsync(usuario);
        return await ObtenerAsync(usuarioId);
    }

    public async Task CambiarPasswordAsync(int usuarioId, CuentaPasswordDto datos)
    {
        var usuario = await ObtenerUsuarioAsync(usuarioId);

        if (_hasher.VerifyHashedPassword(usuario, usuario.PasswordHash, datos.Actual) == PasswordVerificationResult.Failed)
            throw ReglaNegocioException.Invalida("La contraseña actual no es correcta");

        usuario.PasswordHash = _hasher.HashPassword(usuario, datos.Nueva);
        await _usuarioData.ActualizarAsync(usuario);
    }

    private async Task<Usuario> CrearPostulanteAsync(string correo, string nombre, string password)
    {
        var usuario = new Usuario
        {
            NombreUsuario = correo,
            Correo = correo,
            Nombre = nombre,
            Rol = Roles.Postulante,
            FechaRegistro = DateTime.UtcNow,
        };
        usuario.PasswordHash = _hasher.HashPassword(usuario, password);
        await _usuarioData.CrearAsync(usuario);

        await _correo.EncolarAsync(correo, nombre, "Tu cuenta en AMAAC Perú está lista",
            PlantillasCorreo.CuentaCreada(nombre, $"{_correo.FrontUrl}/postular"), TiposCorreo.Bienvenida);

        return usuario;
    }

    private async Task<string?> BuscarDniAsync(Usuario usuario) => usuario.Rol switch
    {
        Roles.Voluntario => (await _voluntarioData.ObtenerPorUsuarioIdAsync(usuario.Id))?.Dni,
        Roles.Practicante => (await _practicanteData.ObtenerPorUsuarioIdAsync(usuario.Id))?.Dni,
        Roles.Postulante => (await _postulacionData.ObtenerUltimaDeUsuarioAsync(usuario.Id))?.Dni,
        _ => null,
    };

    private async Task<Usuario> ObtenerUsuarioAsync(int usuarioId) =>
        await _usuarioData.ObtenerPorIdAsync(usuarioId) ?? throw ReglaNegocioException.NoEncontrada("Cuenta no encontrada");

    private static string NombreDesdeCorreo(string correo) => correo.Split('@')[0];

    /// <summary>El DNI nunca sale completo: solo los 3 últimos dígitos (•••••678).</summary>
    private static string? OcultarDni(string? dni) =>
        string.IsNullOrEmpty(dni) || dni.Length < 3 ? dni : new string('•', dni.Length - 3) + dni[^3..];
}
