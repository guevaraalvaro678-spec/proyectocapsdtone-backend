using System.Security.Claims;
using CapstonePsicologiaAPI.Data;
using CapstonePsicologiaAPI.DTOs;
using CapstonePsicologiaAPI.Models;
using CapstonePsicologiaAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CapstonePsicologiaAPI.Controllers;

[ApiController]
[Route("api/cita")]
public class CitaController : ControllerBase
{
    private const string RolesAdmin = $"{Roles.SuperAdmin},{Roles.Admin}";

    private readonly ICitaService _citaService;

    public CitaController(ICitaService citaService)
    {
        _citaService = citaService;
    }

    /// <summary>Todas las citas, con filtros opcionales. Solo administradores.</summary>
    [Authorize(Roles = RolesAdmin)]
    [HttpGet]
    public async Task<ActionResult<List<CitaDto>>> Listar(
        [FromQuery] string? estado = null,
        [FromQuery] int? psicologoId = null,
        [FromQuery] int? pacienteId = null,
        [FromQuery] DateTime? desde = null,
        [FromQuery] DateTime? hasta = null)
    {
        var filtro = new CitaFiltro(estado, psicologoId, pacienteId, AUtc(desde), AUtc(hasta));
        return Ok(await _citaService.ListarAsync(filtro));
    }

    /// <summary>Mis citas: como paciente si soy usuario, como psicólogo si soy psicólogo.</summary>
    [Authorize(Roles = $"{Roles.Usuario},{Roles.Psicologo}")]
    [HttpGet("mias")]
    public async Task<ActionResult<List<CitaDto>>> ListarMias()
    {
        return Ok(await _citaService.MiasAsync(IdDelToken(), RolDelToken()));
    }

    [Authorize(Roles = RolesAdmin)]
    [HttpPost]
    public async Task<ActionResult<CitaDto>> Crear(CitaCrearDto datos)
    {
        var cita = await _citaService.CrearAsync(datos);
        return StatusCode(StatusCodes.Status201Created, cita);
    }

    [Authorize(Roles = RolesAdmin)]
    [HttpPut("{id:int}")]
    public async Task<ActionResult<CitaDto>> Actualizar(int id, CitaCrearDto datos)
    {
        return Ok(await _citaService.ActualizarAsync(id, datos));
    }

    /// <summary>
    /// Cambia el estado de una cita. Lo que se puede hacer depende del rol: el administrador todo, el
    /// psicólogo sus propias citas y el paciente solo cancelar la suya.
    /// </summary>
    [Authorize]
    [HttpPut("{id:int}/estado")]
    public async Task<ActionResult<CitaDto>> CambiarEstado(int id, CitaEstadoDto datos)
    {
        return Ok(await _citaService.CambiarEstadoAsync(id, datos, IdDelToken(), RolDelToken()));
    }

    private int IdDelToken() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private string RolDelToken() => User.FindFirstValue(ClaimTypes.Role)!;

    /// <summary>Un filtro de fecha sin zona se toma como UTC.</summary>
    private static DateTime? AUtc(DateTime? fecha) => fecha is null
        ? null
        : fecha.Value.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(fecha.Value, DateTimeKind.Utc)
            : fecha.Value.ToUniversalTime();
}
