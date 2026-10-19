using System.Security.Cryptography;
using System.Text;
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
    private readonly ICorreoService _correo;
    private readonly ICorreoEnvio _envioDirecto;
    private readonly ISmsService _sms;
    private readonly ILogger<AdminService> _log;
    private readonly bool _dobleFactor;

    private const int MaximoIntentos = 5;
    private const int MinutosBloqueo = 15;

    /// <summary>Al segundo intento fallido se avisa al dueño de la cuenta por correo.</summary>
    private const int IntentosParaAviso = 2;

    private const int MinutosDesafio = 10;
    private const int MinutosCodigo = 5;
    private const int SegundosEntreCodigos = 30;
    private const int MaximoCodigosIncorrectos = 5;

    public AdminService(
        IAdminData adminData,
        IPasswordHasher<Usuario> hasher,
        ITokenService tokenService,
        IGoogleAuthService google,
        ICorreoService correo,
        ICorreoEnvio envioDirecto,
        ISmsService sms,
        IConfiguration config,
        ILogger<AdminService> log)
    {
        _adminData = adminData;
        _hasher = hasher;
        _tokenService = tokenService;
        _google = google;
        _correo = correo;
        _envioDirecto = envioDirecto;
        _sms = sms;
        _log = log;
        // Se puede apagar en appsettings (Seguridad:DobleFactor = false) si el correo dejara de funcionar.
        _dobleFactor = config.GetValue("Seguridad:DobleFactor", true);
    }

    /// <summary>
    /// Login con usuario o correo. Tras 5 contraseñas incorrectas seguidas la cuenta se bloquea 15 minutos.
    /// Con la contraseña correcta se pide un código de verificación (segundo paso) antes de entregar la sesión.
    /// </summary>
    public async Task<LoginResultadoDto?> LoginAsync(AdminLoginDto login)
    {
        var nombre = Normalizar(login.Usuario);
        var usuario = await _adminData.ObtenerPorUsuarioOCorreoAsync(nombre);

        if (usuario?.BloqueadoHasta > DateTime.UtcNow)
        {
            var minutos = (int)Math.Ceiling((usuario.BloqueadoHasta.Value - DateTime.UtcNow).TotalMinutes);
            throw ReglaNegocioException.Prohibida($"Cuenta bloqueada por intentos fallidos. Intenta de nuevo en {minutos} min.");
        }

        var resultado = usuario is null
            ? PasswordVerificationResult.Failed
            : _hasher.VerifyHashedPassword(usuario, usuario.PasswordHash, login.Password);

        if (usuario is null)
            return null;

        if (resultado == PasswordVerificationResult.Failed)
        {
            await RegistrarFalloAsync(usuario);
            return null;
        }

        // Cuenta con el hash antiguo: se vuelve a guardar en Base64.
        if (resultado == PasswordVerificationResult.SuccessRehashNeeded)
            await _adminData.ActualizarPasswordAsync(usuario.Id, _hasher.HashPassword(usuario, login.Password));

        // Solo se avisa que la cuenta está desactivada a quien ya demostró conocer la contraseña.
        if (!usuario.Activo)
            throw ReglaNegocioException.Prohibida("Tu cuenta está desactivada. Contacta al administrador.");

        var canales = Canales(usuario);

        // Sin verificación en dos pasos, o sin correo ni teléfono donde recibir el código: entra directo.
        if (!_dobleFactor || canales.Count == 0)
        {
            if (_dobleFactor)
                _log.LogWarning("La cuenta {Usuario} no tiene correo ni teléfono: entra sin código de verificación", usuario.NombreUsuario);

            await _adminData.RegistrarAccesoAsync(usuario.Id);
            return new LoginResultadoDto { Sesion = ASesion(usuario, _tokenService) };
        }

        // Contraseña correcta: se crea el desafío del segundo paso.
        var desafio = $"{usuario.Id}.{Convert.ToHexString(RandomNumberGenerator.GetBytes(24))}";
        usuario.DesafioHash = Hash(desafio);
        usuario.DesafioExpira = DateTime.UtcNow.AddMinutes(MinutosDesafio);
        usuario.CodigoVerificacionHash = null;
        usuario.CodigoVerificacionExpira = null;
        usuario.CodigoVerificacionIntentos = 0;
        usuario.IntentosFallidos = 0;
        await _adminData.GuardarAsync();

        return new LoginResultadoDto { RequiereVerificacion = true, Desafio = desafio, Canales = canales };
    }

    public async Task<CodigoEnviadoDto> SolicitarCodigoAsync(CodigoSolicitudDto datos)
    {
        var usuario = await ValidarDesafioAsync(datos.Desafio);
        var canal = Canales(usuario).FirstOrDefault(c => c.Canal == datos.Canal)
                    ?? throw ReglaNegocioException.Invalida("Ese medio no está disponible para tu cuenta");

        // Evita mandar códigos en ráfaga.
        var creadoEn = usuario.CodigoVerificacionExpira?.AddMinutes(-MinutosCodigo);
        if (creadoEn > DateTime.UtcNow.AddSeconds(-SegundosEntreCodigos))
            throw ReglaNegocioException.Conflicto("Espera unos segundos antes de pedir otro código");

        var codigo = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
        usuario.CodigoVerificacionHash = Hash(codigo);
        usuario.CodigoVerificacionExpira = DateTime.UtcNow.AddMinutes(MinutosCodigo);
        usuario.CodigoVerificacionIntentos = 0;
        await _adminData.GuardarAsync();

        // Se envía directo (no pasa por la bandeja de salida) para que el código no quede guardado en el historial.
        try
        {
            if (canal.Canal == "sms")
                await _sms.EnviarAsync(_sms.Normalizar(usuario.Telefono)!, $"AMAAC Peru: tu codigo de verificacion es {codigo}. Vence en {MinutosCodigo} min.");
            else
                await _envioDirecto.EnviarAsync(usuario.Correo!, usuario.Nombre, $"Tu código de verificación: {codigo}",
                    PlantillasCorreo.CodigoVerificacion(usuario.Nombre, codigo, MinutosCodigo), CancellationToken.None);
        }
        catch (ReglaNegocioException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "No se pudo enviar el código de verificación a {Usuario}", usuario.NombreUsuario);
            usuario.CodigoVerificacionExpira = null;
            await _adminData.GuardarAsync();
            throw ReglaNegocioException.Invalida("No se pudo enviar el código. Intenta de nuevo en un momento.");
        }

        return new CodigoEnviadoDto { Destino = canal.Destino, ExpiraEnSegundos = MinutosCodigo * 60 };
    }

    public async Task<AdminSesionDto> VerificarCodigoAsync(CodigoVerificarDto datos)
    {
        var usuario = await ValidarDesafioAsync(datos.Desafio);

        if (usuario.CodigoVerificacionHash is null || usuario.CodigoVerificacionExpira is null || usuario.CodigoVerificacionExpira <= DateTime.UtcNow)
            throw ReglaNegocioException.Invalida("El código venció o no se pidió. Pide uno nuevo.");

        if (!CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(Hash(datos.Codigo)), Encoding.UTF8.GetBytes(usuario.CodigoVerificacionHash)))
        {
            usuario.CodigoVerificacionIntentos++;
            if (usuario.CodigoVerificacionIntentos >= MaximoCodigosIncorrectos)
            {
                LimpiarVerificacion(usuario);
                await _adminData.GuardarAsync();
                throw ReglaNegocioException.Prohibida("Demasiados códigos incorrectos. Vuelve a iniciar sesión.");
            }

            await _adminData.GuardarAsync();
            var quedan = MaximoCodigosIncorrectos - usuario.CodigoVerificacionIntentos;
            throw ReglaNegocioException.Invalida($"Código incorrecto. Te quedan {quedan} {(quedan == 1 ? "intento" : "intentos")}.");
        }

        LimpiarVerificacion(usuario);
        await _adminData.GuardarAsync();
        await _adminData.RegistrarAccesoAsync(usuario.Id);
        return ASesion(usuario, _tokenService);
    }

    /// <summary>Medios por donde la cuenta puede recibir el código: su correo y, si hay proveedor de SMS, su teléfono.</summary>
    private List<CanalVerificacionDto> Canales(Usuario usuario)
    {
        var canales = new List<CanalVerificacionDto>();
        if (!string.IsNullOrWhiteSpace(usuario.Correo))
            canales.Add(new CanalVerificacionDto { Canal = "correo", Destino = OcultarCorreo(usuario.Correo) });

        var telefono = _sms.Disponible ? _sms.Normalizar(usuario.Telefono) : null;
        if (telefono is not null)
            canales.Add(new CanalVerificacionDto { Canal = "sms", Destino = $"*** *** {telefono[^3..]}" });

        return canales;
    }

    private async Task<Usuario> ValidarDesafioAsync(string desafio)
    {
        var separador = desafio.IndexOf('.');
        var usuario = separador > 0 && int.TryParse(desafio[..separador], out var id) ? await _adminData.ObtenerPorIdAsync(id) : null;

        if (usuario?.DesafioHash is null || usuario.DesafioExpira is null || usuario.DesafioExpira <= DateTime.UtcNow
            || !CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(Hash(desafio)), Encoding.UTF8.GetBytes(usuario.DesafioHash)))
            throw ReglaNegocioException.Prohibida("Tu verificación venció. Vuelve a iniciar sesión.");

        return usuario;
    }

    private static void LimpiarVerificacion(Usuario usuario)
    {
        usuario.DesafioHash = null;
        usuario.DesafioExpira = null;
        usuario.CodigoVerificacionHash = null;
        usuario.CodigoVerificacionExpira = null;
        usuario.CodigoVerificacionIntentos = 0;
    }

    private static string Hash(string valor) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(valor)));

    private static string OcultarCorreo(string correo)
    {
        var arroba = correo.IndexOf('@');
        return arroba <= 2 ? $"***{correo[arroba..]}" : $"{correo[..2]}***{correo[arroba..]}";
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

    private async Task RegistrarFalloAsync(Usuario usuario)
    {
        var intentos = usuario.IntentosFallidos + 1;
        if (intentos < MaximoIntentos)
        {
            await _adminData.RegistrarIntentoFallidoAsync(usuario.Id, intentos, null);

            // Segundo intento fallido: se avisa al dueño por si no fue él.
            if (intentos == IntentosParaAviso && usuario.Correo is not null)
            {
                await _correo.EncolarAsync(usuario.Correo, usuario.Nombre, "Alguien intentó entrar a tu cuenta",
                    PlantillasCorreo.AlertaIntentoFallido(usuario.Nombre, DateTime.UtcNow, $"{_correo.FrontUrl}/olvide-password"),
                    TiposCorreo.RecuperarPassword);
            }
            return;
        }

        await _adminData.RegistrarIntentoFallidoAsync(usuario.Id, 0, DateTime.UtcNow.AddMinutes(MinutosBloqueo));
        if (usuario.Correo is not null)
        {
            await _correo.EncolarAsync(usuario.Correo, usuario.Nombre, "Tu cuenta fue bloqueada temporalmente",
                PlantillasCorreo.CuentaBloqueada(usuario.Nombre, MinutosBloqueo, $"{_correo.FrontUrl}/olvide-password"),
                TiposCorreo.RecuperarPassword);
        }
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
