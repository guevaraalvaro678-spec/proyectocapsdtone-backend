using System.Security.Claims;
using CapstonePsicologiaAPI.DTOs;
using CapstonePsicologiaAPI.Models;
using CapstonePsicologiaAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CapstonePsicologiaAPI.Controllers;

[ApiController]
[Route("api/psicologo")]
public class PsicologoController : ControllerBase
{
    private const string RolesAdmin = $"{Roles.SuperAdmin},{Roles.Admin}";

    private readonly IPsicologoService _psicologoService;

    public PsicologoController(IPsicologoService psicologoService)
    {
        _psicologoService = psicologoService;
    }

    /// <summary>Tabla de psicólogos. Solo administradores.</summary>
    [Authorize(Roles = RolesAdmin)]
    [HttpGet]
    public async Task<ActionResult<List<PsicologoListaDto>>> Listar([FromQuery] string? buscar = null, [FromQuery] bool? activo = null)
    {
        return Ok(await _psicologoService.ListarAsync(buscar, activo));
    }

    [Authorize(Roles = RolesAdmin)]
    [HttpGet("{id:int}")]
    public async Task<ActionResult<PsicologoDetalleDto>> ObtenerDetalle(int id)
    {
        return Ok(await _psicologoService.ObtenerDetalleAsync(id));
    }

    [Authorize(Roles = RolesAdmin)]
    [HttpPost]
    public async Task<ActionResult<PsicologoListaDto>> Crear(PsicologoCrearDto datos)
    {
        var psicologo = await _psicologoService.CrearAsync(datos);
        return StatusCode(StatusCodes.Status201Created, psicologo);
    }

    [Authorize(Roles = RolesAdmin)]
    [HttpPut("{id:int}")]
    public async Task<ActionResult<PsicologoListaDto>> Actualizar(int id, PsicologoActualizarDto datos)
    {
        return Ok(await _psicologoService.ActualizarAsync(id, datos));
    }

    /// <summary>Mi resumen como psicólogo: mi perfil, mis próximas citas y mis solicitudes asignadas.</summary>
    [Authorize(Roles = Roles.Psicologo)]
    [HttpGet("yo")]
    public async Task<ActionResult<PsicologoDetalleDto>> ObtenerMiResumen()
    {
        var id = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        return Ok(await _psicologoService.ObtenerDetalleAsync(id));
    }
}
