using System.Security.Cryptography;
using System.Text;
using CapstonePsicologiaAPI.Data;
using CapstonePsicologiaAPI.DTOs;
using CapstonePsicologiaAPI.Models;
using Microsoft.AspNetCore.Identity;

namespace CapstonePsicologiaAPI.Services;

public class UsuarioService : IUsuarioService
{
    private static readonly TimeSpan VigenciaTokenRecuperacion = TimeSpan.FromHours(1);

    private readonly IUsuarioData _usuarioData;
    private readonly ISolicitudData _solicitudData;
    private readonly ICitaData _citaData;
    private readonly IPasswordHasher<Usuario> _hasher;
    private readonly ITokenService _tokenService;
    private readonly ICorreoService _correo;
    private readonly ILogger<UsuarioService> _logger;

    public UsuarioService(
        IUsuarioData usuarioData,
        ISolicitudData solicitudData,
        ICitaData citaData,
        IPasswordHasher<Usuario> hasher,
        ITokenService tokenService,
        ICorreoService correo,
        ILogger<UsuarioService> logger)
    {
        _usuarioData = usuarioData;
        _solicitudData = solicitudData;
        _citaData = citaData;
        _hasher = hasher;
        _tokenService = tokenService;
        _correo = correo;
        _logger = logger;
    }

    public async Task<UsuarioPerfilDto?> ObtenerPerfilAsync(int usuarioId)
    {
        var usuario = await _usuarioData.ObtenerPorIdAsync(usuarioId);
        return usuario is null ? null : APerfil(usuario);
    }

    public async Task<UsuarioPerfilDto?> ActualizarPerfilAsync(int usuarioId, UsuarioPerfilActualizarDto datos)
    {
        var usuario = await _usuarioData.ObtenerPorIdAsync(usuarioId);

        if (usuario is null)
            return null;

        usuario.Nombre = datos.Nombre.Trim();
        usuario.Ubicacion = LimpiarOpcional(datos.Ubicacion);
        usuario.Intereses = LimpiarOpcional(datos.Intereses);
        usuario.RecordatoriosCorreo = datos.RecordatoriosCorreo;
        usuario.PreferenciaVirtual = datos.PreferenciaVirtual;

        return APerfil(usuario);
    }

    // ---- Recuperación de contraseña ----

    public async Task SolicitarRecuperacionAsync(string correo)
    {
        var direccion = correo.Trim().ToLowerInvariant();
        var usuario = await _usuarioData.ObtenerPorCorreoAsync(direccion);

        // Sin cuenta (o desactivada) no se hace nada, y la respuesta al cliente es la misma.
        if (usuario is null || !usuario.Activo)
            return;

        var token = Base64Url(RandomNumberGenerator.GetBytes(32));
        usuario.TokenRecuperacionHash = HashToken(token);
        usuario.TokenRecuperacionExpira = DateTime.UtcNow + VigenciaTokenRecuperacion;
        await _usuarioData.ActualizarAsync(usuario);

        var enlace = $"{_correo.FrontUrl}/restablecer?correo={Uri.EscapeDataString(direccion)}&token={token}";

        if (!_correo.SmtpConfigurado)
        {
            // Solo en la consola del servidor: sin SMTP el correo no sale y así se puede probar el flujo.
            _logger.LogWarning("SMTP sin configurar. Enlace de recuperación para {Correo}: {Enlace}", direccion, enlace);
        }

        await _correo.EncolarAsync(
            direccion,
            usuario.Nombre,
            "Restablece tu contraseña",
            PlantillasCorreo.RecuperarPassword(usuario.Nombre, enlace),
            TiposCorreo.RecuperarPassword);
    }

    public async Task RestablecerPasswordAsync(RestablecerPasswordDto datos)
    {
        var usuario = await _usuarioData.ObtenerPorCorreoAsync(datos.Correo.Trim().ToLowerInvariant());

        var valido = usuario is { Activo: true, TokenRecuperacionHash: not null, TokenRecuperacionExpira: not null }
                     && usuario.TokenRecuperacionExpira > DateTime.UtcNow
                     && CryptographicOperations.FixedTimeEquals(
                         Encoding.UTF8.GetBytes(usuario.TokenRecuperacionHash),
                         Encoding.UTF8.GetBytes(HashToken(datos.Token)));

        if (!valido || usuario is null)
            throw ReglaNegocioException.Invalida("El enlace no es válido o ya venció. Pide uno nuevo.");

        usuario.PasswordHash = _hasher.HashPassword(usuario, datos.NuevaPassword);

        // El enlace sirve una sola vez.
        usuario.TokenRecuperacionHash = null;
        usuario.TokenRecuperacionExpira = null;
        await _usuarioData.ActualizarAsync(usuario);
    }

    // ---- Administración ----

    public async Task<List<UsuarioListaDto>> ListarAsync(string tipo, string? buscar, bool? activo, string actorRol)
    {
        var roles = RolesDelTipo(tipo, actorRol);
        var usuarios = await _usuarioData.ListarAsync(roles, buscar, activo);
        return usuarios.Select(AListaDto).ToList();
    }

    public async Task<UsuarioDetalleDto> ObtenerDetalleAsync(int id, string actorRol)
    {
        var usuario = await _usuarioData.ObtenerPorIdAsync(id)
                      ?? throw ReglaNegocioException.NoEncontrada("Usuario no encontrado");

        // Los psicólogos tienen su propio detalle; el de las cuentas de administrador es solo del SuperAdmin.
        if (usuario.Rol == Roles.Psicologo)
            throw ReglaNegocioException.NoEncontrada("Usuario no encontrado");

        ValidarAccesoARol(usuario.Rol, actorRol);

        var solicitudes = await _solicitudData.ObtenerDeUsuarioAsync(id);
        var citas = await _citaData.ListarAsync(new CitaFiltro(PacienteId: id));

        var detalle = new UsuarioDetalleDto
        {
            Ubicacion = usuario.Ubicacion,
            Intereses = usuario.Intereses,
            RecordatoriosCorreo = usuario.RecordatoriosCorreo,
            PreferenciaVirtual = usuario.PreferenciaVirtual,
            Solicitudes = solicitudes.Select(Mapeos.ASolicitud).ToList(),
            Citas = citas.Select(c => Mapeos.ACita(c, incluirNotas: true)).ToList(),
        };

        CopiarLista(usuario, detalle);
        return detalle;
    }

    public async Task<UsuarioListaDto> CambiarActivoAsync(int id, bool activo, int actorId, string actorRol)
    {
        var usuario = await _usuarioData.ObtenerPorIdAsync(id)
                      ?? throw ReglaNegocioException.NoEncontrada("Usuario no encontrado");

        if (id == actorId)
            throw ReglaNegocioException.Invalida("No puedes desactivar tu propia cuenta");

        ValidarAccesoARol(usuario.Rol, actorRol);

        usuario.Activo = activo;
        await _usuarioData.ActualizarAsync(usuario);

        return AListaDto(usuario);
    }

    // ---- Internos ----

    private static string[] RolesDelTipo(string tipo, string actorRol)
    {
        if (tipo == "Administrador")
        {
            if (actorRol != Roles.SuperAdmin)
                throw ReglaNegocioException.Prohibida("Solo un SuperAdmin puede ver las cuentas de administrador");

            return new[] { Roles.SuperAdmin, Roles.Admin };
        }

        return new[] { Roles.Voluntario, Roles.Practicante, Roles.RRHH };
    }

    /// <summary>Un Admin puede gestionar usuarios y psicólogos; las cuentas de administrador, solo un SuperAdmin.</summary>
    private static void ValidarAccesoARol(string rolDelUsuario, string actorRol)
    {
        var esCuentaAdmin = rolDelUsuario is Roles.SuperAdmin or Roles.Admin;

        if (esCuentaAdmin && actorRol != Roles.SuperAdmin)
            throw ReglaNegocioException.Prohibida("Solo un SuperAdmin puede gestionar cuentas de administrador");
    }

    private static UsuarioListaDto AListaDto(Usuario u)
    {
        var dto = new UsuarioListaDto();
        CopiarLista(u, dto);
        return dto;
    }

    private static void CopiarLista(Usuario u, UsuarioListaDto destino)
    {
        destino.Id = u.Id;
        destino.Nombre = string.IsNullOrWhiteSpace(u.Nombre) ? u.NombreUsuario : u.Nombre;
        destino.Correo = u.Correo;
        destino.NombreUsuario = u.NombreUsuario;
        destino.Telefono = u.Telefono;
        destino.Rol = u.Rol;
        destino.Activo = u.Activo;
        destino.FechaRegistro = Mapeos.Utc(u.FechaRegistro);
        destino.UltimoAcceso = u.UltimoAcceso is null ? null : Mapeos.Utc(u.UltimoAcceso.Value);
    }

    private static string? LimpiarOpcional(string? valor) =>
        string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();

    private static UsuarioPerfilDto APerfil(Usuario u) => new()
    {
        Id = u.Id,
        Nombre = u.Nombre,
        Correo = u.Correo,
        Ubicacion = u.Ubicacion,
        Intereses = u.Intereses,
        RecordatoriosCorreo = u.RecordatoriosCorreo,
        PreferenciaVirtual = u.PreferenciaVirtual,
        FechaRegistro = Mapeos.Utc(u.FechaRegistro),
    };

    private static string Base64Url(byte[] bytes) =>
        Convert.ToBase64String(bytes).Replace('+', '-').Replace('/', '_').TrimEnd('=');

    private static string HashToken(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
