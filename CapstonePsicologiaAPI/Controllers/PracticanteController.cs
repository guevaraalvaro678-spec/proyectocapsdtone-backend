using System.Security.Claims;
using CapstonePsicologiaAPI.DTOs;
using CapstonePsicologiaAPI.Models;
using CapstonePsicologiaAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CapstonePsicologiaAPI.Controllers;

[ApiController]
[Route("api/practicante")]
public class PracticanteController : ControllerBase
{
    private const string RolesGestion = $"{Roles.SuperAdmin},{Roles.Admin},{Roles.RRHH}";

    private readonly IPracticanteService _practicanteService;

    public PracticanteController(IPracticanteService practicanteService)
    {
        _practicanteService = practicanteService;
    }

    /// <summary>Alta directa de un practicante. Solo la dirección: el camino normal es postular y ser aceptado.</summary>
    [Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Admin}")]
    [HttpPost("registro")]
    public async Task<ActionResult<AdminSesionDto>> Registrar([FromBody] PracticanteRegistroDto datos) =>
        StatusCode(StatusCodes.Status201Created, await _practicanteService.RegistrarAsync(datos));

    [Authorize(Roles = Roles.Practicante)]
    [HttpGet("yo")]
    public async Task<ActionResult<PracticanteDto>> Yo() =>
        Ok(await _practicanteService.ObtenerYoAsync(UsuarioId()));

    /// <summary>El practicante envía su horario de la universidad; queda Pendiente de revisión.</summary>
    [Authorize(Roles = Roles.Practicante)]
    [HttpPut("yo/horario")]
    public async Task<ActionResult<PracticanteDto>> EnviarHorario([FromBody] HorarioEnviarDto datos) =>
        Ok(await _practicanteService.EnviarHorarioAsync(UsuarioId(), datos));

    /// <summary>El practicante confirma el horario de prácticas asignado.</summary>
    [Authorize(Roles = Roles.Practicante)]
    [HttpPut("yo/horario/confirmar")]
    public async Task<ActionResult<PracticanteDto>> ConfirmarHorario() =>
        Ok(await _practicanteService.ConfirmarHorarioAsync(UsuarioId()));

    [Authorize(Roles = RolesGestion)]
    [HttpGet]
    public async Task<ActionResult<List<PracticanteDto>>> Listar([FromQuery] string? estado = null, [FromQuery] string? buscar = null) =>
        Ok(await _practicanteService.ListarAsync(estado, buscar));

    /// <summary>Recursos Humanos pone el horario en revisión o lo acepta asignando el horario de prácticas.</summary>
    [Authorize(Roles = RolesGestion)]
    [HttpPut("{usuarioId:int}/horario")]
    public async Task<ActionResult<PracticanteDto>> RevisarHorario(int usuarioId, [FromBody] HorarioRevisionDto datos) =>
        Ok(await _practicanteService.RevisarHorarioAsync(usuarioId, datos, UsuarioId()));

    private int UsuarioId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
