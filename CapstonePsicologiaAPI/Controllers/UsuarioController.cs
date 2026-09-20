using System.Security.Claims;
using CapstonePsicologiaAPI.DTOs;
using CapstonePsicologiaAPI.Models;
using CapstonePsicologiaAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CapstonePsicologiaAPI.Controllers;

[ApiController]
[Route("api/usuario")]
public class UsuarioController : ControllerBase
{
    private const string RolesAdmin = $"{Roles.SuperAdmin},{Roles.Admin}";

    private readonly IUsuarioService _usuarioService;

    public UsuarioController(IUsuarioService usuarioService)
    {
        _usuarioService = usuarioService;
    }

    // ---- Público ----

    /// <summary>Registro público de un usuario común. Devuelve la sesión, así que queda logueado.</summary>
    [HttpPost("registro")]
    public async Task<ActionResult<AdminSesionDto>> Registro(UsuarioRegistroDto registro)
    {
        var sesion = await _usuarioService.RegistrarAsync(registro);

        if (sesion is null)
            return Conflict(new { mensaje = "Ese correo ya está registrado" });

        return StatusCode(StatusCodes.Status201Created, sesion);
    }

    /// <summary>Envía el enlace para restablecer la contraseña. Responde igual exista o no la cuenta.</summary>
    [HttpPost("olvide-password")]
    public async Task<IActionResult> OlvidePassword(RecuperarPasswordDto datos)
    {
        await _usuarioService.SolicitarRecuperacionAsync(datos.Correo);
        return NoContent();
    }

    [HttpPost("restablecer-password")]
    public async Task<IActionResult> RestablecerPassword(RestablecerPasswordDto datos)
    {
        await _usuarioService.RestablecerPasswordAsync(datos);
        return NoContent();
    }

    // ---- Cualquier cuenta con sesión: su propio perfil ----

    [Authorize]
    [HttpGet("perfil")]
    public async Task<ActionResult<UsuarioPerfilDto>> ObtenerPerfil()
    {
        var perfil = await _usuarioService.ObtenerPerfilAsync(IdDelToken());
        return perfil is null ? NotFound() : Ok(perfil);
    }

    [Authorize]
    [HttpPut("perfil")]
    public async Task<ActionResult<UsuarioPerfilDto>> ActualizarPerfil(UsuarioPerfilActualizarDto datos)
    {
        var perfil = await _usuarioService.ActualizarPerfilAsync(IdDelToken(), datos);
        return perfil is null ? NotFound() : Ok(perfil);
    }

    // ---- Administración ----

    /// <summary>Tabla de usuarios. `tipo`: "Usuario" (por defecto) o "Administrador" (solo SuperAdmin).</summary>
    [Authorize(Roles = RolesAdmin)]
    [HttpGet]
    public async Task<ActionResult<List<UsuarioListaDto>>> Listar([FromQuery] string tipo = "Usuario", [FromQuery] string? buscar = null, [FromQuery] bool? activo = null)
    {
        return Ok(await _usuarioService.ListarAsync(tipo, buscar, activo, RolDelToken()));
    }

    [Authorize(Roles = RolesAdmin)]
    [HttpGet("{id:int}")]
    public async Task<ActionResult<UsuarioDetalleDto>> ObtenerDetalle(int id)
    {
        return Ok(await _usuarioService.ObtenerDetalleAsync(id, RolDelToken()));
    }

    /// <summary>Activa o desactiva una cuenta. Una cuenta desactivada no puede iniciar sesión.</summary>
    [Authorize(Roles = RolesAdmin)]
    [HttpPut("{id:int}/activo")]
    public async Task<ActionResult<UsuarioListaDto>> CambiarActivo(int id, UsuarioActivoDto datos)
    {
        return Ok(await _usuarioService.CambiarActivoAsync(id, datos.Activo, IdDelToken(), RolDelToken()));
    }

    /// <summary>El id y el rol salen del token, nunca de lo que envía el cliente.</summary>
    private int IdDelToken() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private string RolDelToken() => User.FindFirstValue(ClaimTypes.Role)!;
}
