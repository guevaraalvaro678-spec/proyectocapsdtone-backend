using System.Security.Claims;
using CapstonePsicologiaAPI.DTOs;
using CapstonePsicologiaAPI.Models;
using CapstonePsicologiaAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CapstonePsicologiaAPI.Controllers;

[ApiController]
[Route("api/marcacion")]
public class MarcacionController : ControllerBase
{
    private const string RolesGestion = $"{Roles.SuperAdmin},{Roles.Admin},{Roles.RRHH}";

    private readonly IMarcacionService _marcacionService;

    public MarcacionController(IMarcacionService marcacionService)
    {
        _marcacionService = marcacionService;
    }

    [Authorize(Roles = Roles.Practicante)]
    [HttpPost("entrada")]
    public async Task<ActionResult<MarcacionDto>> Entrada() =>
        Ok(await _marcacionService.MarcarEntradaAsync(UsuarioId()));

    [Authorize(Roles = Roles.Practicante)]
    [HttpPost("salida")]
    public async Task<ActionResult<MarcacionDto>> Salida() =>
        Ok(await _marcacionService.MarcarSalidaAsync(UsuarioId()));

    [Authorize(Roles = Roles.Practicante)]
    [HttpGet("mias")]
    public async Task<ActionResult<List<MarcacionDto>>> Mias() =>
        Ok(await _marcacionService.MiasAsync(UsuarioId()));

    /// <summary>Asistencia de todos los practicantes; por defecto solo la de hoy (hora de Lima).</summary>
    [Authorize(Roles = RolesGestion)]
    [HttpGet]
    public async Task<ActionResult<List<MarcacionDto>>> Listar([FromQuery] bool soloHoy = true, [FromQuery] int? usuarioId = null) =>
        Ok(await _marcacionService.ListarAsync(soloHoy, usuarioId));

    private int UsuarioId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
