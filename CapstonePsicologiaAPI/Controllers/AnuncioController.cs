using System.Security.Claims;
using CapstonePsicologiaAPI.DTOs;
using CapstonePsicologiaAPI.Models;
using CapstonePsicologiaAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CapstonePsicologiaAPI.Controllers;

/// <summary>Anuncios del sitio público.</summary>
[ApiController]
[Route("api/anuncio")]
[Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Admin}")]
public class AnuncioController : ControllerBase
{
    private readonly IAnuncioService _anuncioService;

    public AnuncioController(IAnuncioService anuncioService)
    {
        _anuncioService = anuncioService;
    }

    /// <summary>Los que el público ve ahora (activos y dentro de su vigencia).</summary>
    [AllowAnonymous]
    [HttpGet("vigentes")]
    public async Task<ActionResult<List<AnuncioDto>>> Vigentes() => Ok(await _anuncioService.ListarVigentesAsync());

    [HttpGet]
    public async Task<ActionResult<List<AnuncioDto>>> Listar() => Ok(await _anuncioService.ListarAsync());

    [HttpPost]
    public async Task<ActionResult<AnuncioDto>> Crear([FromBody] AnuncioGuardarDto datos) =>
        StatusCode(StatusCodes.Status201Created, await _anuncioService.CrearAsync(datos));

    [HttpPut("{id:int}")]
    public async Task<ActionResult<AnuncioDto>> Actualizar(int id, [FromBody] AnuncioGuardarDto datos) =>
        Ok(await _anuncioService.ActualizarAsync(id, datos));

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Eliminar(int id)
    {
        await _anuncioService.EliminarAsync(id);
        return NoContent();
    }
}

/// <summary>Alertas de seguridad y copias de seguridad de la base.</summary>
[ApiController]
[Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Admin}")]
public class SeguridadController : ControllerBase
{
    private readonly IAlertaService _alertaService;
    private readonly IRespaldoService _respaldoService;

    public SeguridadController(IAlertaService alertaService, IRespaldoService respaldoService)
    {
        _alertaService = alertaService;
        _respaldoService = respaldoService;
    }

    [HttpGet("api/alerta")]
    public async Task<ActionResult<AlertasResumenDto>> Alertas([FromQuery] bool soloPendientes = false) =>
        Ok(await _alertaService.ListarAsync(soloPendientes));

    [HttpPut("api/alerta/{id:int}/revisada")]
    public async Task<IActionResult> MarcarRevisada(int id)
    {
        await _alertaService.MarcarRevisadaAsync(id, UsuarioId());
        return NoContent();
    }

    [HttpPut("api/alerta/revisadas")]
    public async Task<IActionResult> MarcarTodasRevisadas()
    {
        await _alertaService.MarcarTodasRevisadasAsync(UsuarioId());
        return NoContent();
    }

    [HttpGet("api/respaldo")]
    public async Task<ActionResult<RespaldoEstadoDto>> Respaldos() => Ok(await _respaldoService.EstadoAsync());

    /// <summary>Copia de seguridad inmediata ("Respaldar ahora").</summary>
    [HttpPost("api/respaldo")]
    public async Task<ActionResult<RespaldoDto>> Respaldar() =>
        StatusCode(StatusCodes.Status201Created, await _respaldoService.RespaldarAsync());

    private int UsuarioId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
