using CapstonePsicologiaAPI.DTOs;
using CapstonePsicologiaAPI.Models;
using CapstonePsicologiaAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CapstonePsicologiaAPI.Controllers;

[ApiController]
[Route("api/admin")]
public class AdminController : ControllerBase
{
    private readonly IAdminService _adminService;

    public AdminController(IAdminService adminService)
    {
        _adminService = adminService;
    }

    /// <summary>
    /// Primer paso: usuario y contraseña. Con verificación en dos pasos devuelve un desafío y los medios
    /// para recibir el código; la sesión llega después, en login/verificar.
    /// </summary>
    [HttpPost("login")]
    public async Task<ActionResult<LoginResultadoDto>> Login(AdminLoginDto login)
    {
        var resultado = await _adminService.LoginAsync(login);

        if (resultado is null)
            return Unauthorized(new { mensaje = "Usuario o contraseña incorrectos" });

        return Ok(resultado);
    }

    /// <summary>Segundo paso: envía el código de 6 dígitos por correo o SMS.</summary>
    [HttpPost("login/codigo")]
    public async Task<ActionResult<CodigoEnviadoDto>> SolicitarCodigo(CodigoSolicitudDto datos) =>
        Ok(await _adminService.SolicitarCodigoAsync(datos));

    /// <summary>Segundo paso: comprueba el código y entrega la sesión.</summary>
    [HttpPost("login/verificar")]
    public async Task<ActionResult<AdminSesionDto>> VerificarCodigo(CodigoVerificarDto datos)
    {
        return Ok(await _adminService.VerificarCodigoAsync(datos));
    }

    /// <summary>Iniciar sesión con Google (la cuenta se crea antes desde "Únete").</summary>
    [HttpPost("google")]
    public async Task<ActionResult<AdminSesionDto>> LoginGoogle(GoogleCredencialDto datos) =>
        Ok(await _adminService.LoginGoogleAsync(datos.Credential));

    /// <summary>Solo un SuperAdmin logueado puede crear nuevos usuarios.</summary>
    [Authorize(Roles = Roles.SuperAdmin)]
    [HttpPost("registro")]
    public async Task<ActionResult<AdminDto>> Registro(AdminRegistroDto registro)
    {
        var admin = await _adminService.RegistrarAsync(registro);

        if (admin is null)
            return Conflict(new { mensaje = "Ese nombre de usuario ya existe" });

        return StatusCode(StatusCodes.Status201Created, admin);
    }
}
