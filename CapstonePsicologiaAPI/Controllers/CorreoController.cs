using System.Security.Claims;
using CapstonePsicologiaAPI.DTOs;
using CapstonePsicologiaAPI.Models;
using CapstonePsicologiaAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CapstonePsicologiaAPI.Controllers;

/// <summary>Correos y notificaciones que controla el administrador. Todo el controlador es solo para administradores.</summary>
[ApiController]
[Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Admin}")]
[Route("api/correo")]
public class CorreoController : ControllerBase
{
    private readonly ICorreoService _correoService;

    public CorreoController(ICorreoService correoService)
    {
        _correoService = correoService;
    }

    /// <summary>Historial de la bandeja de salida (los últimos 200 correos).</summary>
    [HttpGet]
    public async Task<ActionResult<List<CorreoDto>>> Historial()
    {
        return Ok(await _correoService.HistorialAsync());
    }

    /// <summary>Si el servidor tiene SMTP configurado y cuántos correos esperan salir.</summary>
    [HttpGet("estado")]
    public async Task<ActionResult<CorreoEstadoDto>> ObtenerEstado()
    {
        return Ok(await _correoService.ObtenerEstadoAsync());
    }

    /// <summary>Envía un correo a una persona o una notificación masiva a un grupo.</summary>
    [HttpPost("enviar")]
    public async Task<ActionResult<CorreoEnvioResultadoDto>> Enviar(CorreoEnviarDto datos)
    {
        var adminId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var resultado = await _correoService.EnviarAsync(datos, adminId);

        if (resultado is null)
            return BadRequest(new { mensaje = "No hay destinatarios para esa audiencia" });

        return StatusCode(StatusCodes.Status201Created, resultado);
    }

    /// <summary>Vuelve a poner en la cola un correo que falló.</summary>
    [HttpPost("{id:int}/reintentar")]
    public async Task<IActionResult> Reintentar(int id)
    {
        var reintentado = await _correoService.ReintentarAsync(id);

        return reintentado
            ? NoContent()
            : NotFound(new { mensaje = "El correo no existe o no estaba fallido" });
    }
}
