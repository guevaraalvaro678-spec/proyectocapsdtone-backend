using CapstonePsicologiaAPI.Data;
using CapstonePsicologiaAPI.DTOs;
using CapstonePsicologiaAPI.Models;
using CapstonePsicologiaAPI.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace CapstonePsicologiaAPI.Tests;

/// <summary>Inicio de sesión: contraseñas (HU-02), bloqueo por intentos (HU-06) y código de verificación (HU-04).</summary>
public class SeguridadTests
{
    private readonly Mock<IAdminData> _datos = new();
    private readonly Mock<ICorreoService> _correo = new();
    private readonly Mock<ICorreoEnvio> _envio = new();
    private readonly Mock<ITokenService> _token = new();
    private readonly ContrasenaBase64 _hasher = new();

    private AdminService Servicio() => new(_datos.Object, _hasher, _token.Object, Mock.Of<IGoogleAuthService>(), _correo.Object,
        _envio.Object, Mock.Of<ISmsService>(), new ConfigurationBuilder().Build(), NullLogger<AdminService>.Instance);

    private Usuario Cuenta(int intentosFallidos = 0, bool activo = true, DateTime? bloqueadoHasta = null)
    {
        var usuario = new Usuario
        {
            Id = 7, NombreUsuario = "ana@correo.com", Nombre = "Ana", Correo = "ana@correo.com", Rol = Roles.RRHH,
            IntentosFallidos = intentosFallidos, Activo = activo, BloqueadoHasta = bloqueadoHasta,
        };
        usuario.PasswordHash = _hasher.HashPassword(usuario, "Clave123");
        _datos.Setup(d => d.ObtenerPorUsuarioOCorreoAsync("ana@correo.com")).ReturnsAsync(usuario);
        _datos.Setup(d => d.ObtenerPorIdAsync(7)).ReturnsAsync(usuario);
        return usuario;
    }

    private static AdminLoginDto Login(string password) => new() { Usuario = "ana@correo.com", Password = password };

    // ---- Contraseñas ----

    [Fact]
    public void Contrasena_se_guarda_codificada_en_Base64()
    {
        Assert.Equal("MTIz", _hasher.HashPassword(new Usuario(), "123"));
    }

    [Fact]
    public void Contrasena_correcta_se_acepta_e_incorrecta_se_rechaza()
    {
        var hash = _hasher.HashPassword(new Usuario(), "Clave123");
        Assert.Equal(PasswordVerificationResult.Success, _hasher.VerifyHashedPassword(new Usuario(), hash, "Clave123"));
        Assert.Equal(PasswordVerificationResult.Failed, _hasher.VerifyHashedPassword(new Usuario(), hash, "otra"));
    }

    [Fact]
    public void Cuenta_con_hash_antiguo_PBKDF2_entra_y_se_vuelve_a_guardar()
    {
        var antiguo = new PasswordHasher<Usuario>().HashPassword(new Usuario(), "Clave123");
        Assert.Equal(PasswordVerificationResult.SuccessRehashNeeded, _hasher.VerifyHashedPassword(new Usuario(), antiguo, "Clave123"));
    }

    // ---- Bloqueo por intentos ----

    [Fact]
    public async Task Contrasena_incorrecta_no_entra_y_registra_el_intento()
    {
        Cuenta();
        Assert.Null(await Servicio().LoginAsync(Login("mala")));
        _datos.Verify(d => d.RegistrarIntentoFallidoAsync(7, 1, null), Times.Once);
    }

    [Fact]
    public async Task Segundo_intento_fallido_avisa_al_dueno_por_correo()
    {
        Cuenta(intentosFallidos: 1);
        await Servicio().LoginAsync(Login("mala"));
        _correo.Verify(c => c.EncolarAsync("ana@correo.com", "Ana", "Alguien intentó entrar a tu cuenta",
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<bool>()), Times.Once);
    }

    [Fact]
    public async Task Quinto_intento_fallido_bloquea_la_cuenta_15_minutos()
    {
        Cuenta(intentosFallidos: 4);
        await Servicio().LoginAsync(Login("mala"));
        _datos.Verify(d => d.RegistrarIntentoFallidoAsync(7, 0,
            It.Is<DateTime?>(h => h > DateTime.UtcNow.AddMinutes(14) && h <= DateTime.UtcNow.AddMinutes(15))), Times.Once);
    }

    [Fact]
    public async Task Cuenta_bloqueada_no_puede_entrar_aunque_la_contrasena_sea_correcta()
    {
        Cuenta(bloqueadoHasta: DateTime.UtcNow.AddMinutes(10));
        var error = await Assert.ThrowsAsync<ReglaNegocioException>(() => Servicio().LoginAsync(Login("Clave123")));
        Assert.Equal(403, error.Codigo);
    }

    [Fact]
    public async Task Cuenta_desactivada_no_puede_entrar()
    {
        Cuenta(activo: false);
        var error = await Assert.ThrowsAsync<ReglaNegocioException>(() => Servicio().LoginAsync(Login("Clave123")));
        Assert.Equal(403, error.Codigo);
    }

    // ---- Verificación en dos pasos ----

    [Fact]
    public async Task Contrasena_correcta_pide_el_codigo_antes_de_dar_la_sesion()
    {
        Cuenta();
        var resultado = await Servicio().LoginAsync(Login("Clave123"));

        Assert.NotNull(resultado);
        Assert.True(resultado.RequiereVerificacion);
        Assert.Null(resultado.Sesion);
        Assert.False(string.IsNullOrEmpty(resultado.Desafio));
        Assert.Contains(resultado.Canales, c => c.Canal == "correo");
    }

    private async Task<(AdminService Servicio, string Desafio, string Codigo)> PedirCodigoAsync()
    {
        string codigo = string.Empty;
        _envio.Setup(e => e.EnviarAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
              .Callback<string, string?, string, string, CancellationToken>((_, _, asunto, _, _) => codigo = asunto[^6..])
              .Returns(Task.CompletedTask);

        Cuenta();
        var servicio = Servicio();
        var desafio = (await servicio.LoginAsync(Login("Clave123")))!.Desafio!;
        var enviado = await servicio.SolicitarCodigoAsync(new CodigoSolicitudDto { Desafio = desafio, Canal = "correo" });

        Assert.Equal(300, enviado.ExpiraEnSegundos);
        Assert.Matches(@"^\d{6}$", codigo);
        return (servicio, desafio, codigo);
    }

    [Fact]
    public async Task Codigo_correcto_de_6_digitos_entrega_la_sesion()
    {
        _token.Setup(t => t.Generar(It.IsAny<Usuario>())).Returns("token-jwt");
        var (servicio, desafio, codigo) = await PedirCodigoAsync();

        var sesion = await servicio.VerificarCodigoAsync(new CodigoVerificarDto { Desafio = desafio, Codigo = codigo });

        Assert.Equal("token-jwt", sesion.Token);
        Assert.Equal(Roles.RRHH, sesion.Rol);
    }

    [Fact]
    public async Task Codigo_incorrecto_avisa_cuantos_intentos_quedan()
    {
        var (servicio, desafio, codigo) = await PedirCodigoAsync();
        var incorrecto = codigo == "000000" ? "111111" : "000000";

        var error = await Assert.ThrowsAsync<ReglaNegocioException>(() =>
            servicio.VerificarCodigoAsync(new CodigoVerificarDto { Desafio = desafio, Codigo = incorrecto }));
        Assert.Contains("Te quedan 4 intentos", error.Message);
    }

    [Fact]
    public async Task Cinco_codigos_incorrectos_anulan_la_verificacion()
    {
        var (servicio, desafio, codigo) = await PedirCodigoAsync();
        var incorrecto = codigo == "000000" ? "111111" : "000000";
        var datos = new CodigoVerificarDto { Desafio = desafio, Codigo = incorrecto };

        for (var i = 0; i < 4; i++)
            await Assert.ThrowsAsync<ReglaNegocioException>(() => servicio.VerificarCodigoAsync(datos));

        var error = await Assert.ThrowsAsync<ReglaNegocioException>(() => servicio.VerificarCodigoAsync(datos));
        Assert.Equal(403, error.Codigo);

        // Ni siquiera el código correcto sirve ya: hay que volver a iniciar sesión.
        await Assert.ThrowsAsync<ReglaNegocioException>(() =>
            servicio.VerificarCodigoAsync(new CodigoVerificarDto { Desafio = desafio, Codigo = codigo }));
    }
}
