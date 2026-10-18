using System.Security.Claims;
using CapstonePsicologiaAPI.DTOs;
using CapstonePsicologiaAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CapstonePsicologiaAPI.Controllers;

/// <summary>Cuenta propia: crear cuenta, entrar con Google y "Mi cuenta y seguridad".</summary>
[ApiController]
[Route("api/cuenta")]
public class CuentaController : ControllerBase
{
    private readonly ICuentaService _cuentaService;

    public CuentaController(ICuentaService cuentaService)
    {
        _cuentaService = cuentaService;
    }

    /// <summary>"Crea tu cuenta": correo y contraseña. Devuelve la sesión (rol Postulante).</summary>
    [HttpPost("registro")]
    public async Task<ActionResult<AdminSesionDto>> Registrar([FromBody] CuentaRegistroDto datos) =>
        StatusCode(StatusCodes.Status201Created, await _cuentaService.RegistrarAsync(datos));

    /// <summary>Entrar con Google. Con crear=false responde 404 si el correo no tiene cuenta (no la crea).</summary>
    [HttpPost("google")]
    public async Task<ActionResult<AdminSesionDto>> Google([FromBody] GoogleCredencialDto datos, [FromQuery] bool crear = true) =>
        Ok(await _cuentaService.EntrarConGoogleAsync(datos.Credential, crear));

    /// <summary>Token nuevo con el rol actual (por ejemplo, después de que aceptan la postulación).</summary>
    [Authorize]
    [HttpPost("renovar")]
    public async Task<ActionResult<AdminSesionDto>> Renovar() =>
        Ok(await _cuentaService.RenovarSesionAsync(UsuarioId()));

    [Authorize]
    [HttpGet("yo")]
    public async Task<ActionResult<CuentaDto>> Yo() =>
        Ok(await _cuentaService.ObtenerAsync(UsuarioId()));

    [Authorize]
    [HttpPut("perfil")]
    public async Task<ActionResult<CuentaDto>> ActualizarPerfil([FromBody] CuentaPerfilDto datos) =>
        Ok(await _cuentaService.ActualizarPerfilAsync(UsuarioId(), datos));

    [Authorize]
    [HttpPut("password")]
    public async Task<IActionResult> CambiarPassword([FromBody] CuentaPasswordDto datos)
    {
        await _cuentaService.CambiarPasswordAsync(UsuarioId(), datos);
        return NoContent();
    }

    private int UsuarioId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
