using System.Security.Claims;
using CapstonePsicologiaAPI.DTOs;
using CapstonePsicologiaAPI.Models;
using CapstonePsicologiaAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CapstonePsicologiaAPI.Controllers;

/// <summary>Reserva de citas por parte del paciente: elige psicólogo, ve sus horarios libres y reserva.</summary>
[ApiController]
[Authorize(Roles = Roles.Usuario)]
[Route("api/reserva")]
public class ReservaController : ControllerBase
{
    private readonly IReservaService _reservaService;

    public ReservaController(IReservaService reservaService)
    {
        _reservaService = reservaService;
    }

    /// <summary>Psicólogos con horarios disponibles.</summary>
    [HttpGet("psicologos")]
    public async Task<ActionResult<List<PsicologoPublicoDto>>> ListarPsicologos()
    {
        return Ok(await _reservaService.ListarPsicologosAsync());
    }

    /// <summary>Horarios libres de un psicólogo en los próximos días (por defecto 14, máximo 30).</summary>
    [HttpGet("horarios")]
    public async Task<ActionResult<List<HorarioDisponibleDto>>> ListarHorarios([FromQuery] int psicologoId, [FromQuery] int dias = 14)
    {
        return Ok(await _reservaService.ListarHorariosAsync(psicologoId, dias));
    }

    [HttpPost]
    public async Task<ActionResult<CitaDto>> Reservar(ReservaCrearDto datos)
    {
        var pacienteId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var cita = await _reservaService.ReservarAsync(pacienteId, datos);
        return StatusCode(StatusCodes.Status201Created, cita);
    }
}
