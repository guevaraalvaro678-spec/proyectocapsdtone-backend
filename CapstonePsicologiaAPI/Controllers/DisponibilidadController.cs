using System.Security.Claims;
using CapstonePsicologiaAPI.DTOs;
using CapstonePsicologiaAPI.Models;
using CapstonePsicologiaAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CapstonePsicologiaAPI.Controllers;

/// <summary>Los horarios que cada psicólogo deja disponibles para que los pacientes reserven.</summary>
[ApiController]
[Route("api/disponibilidad")]
public class DisponibilidadController : ControllerBase
{
    private readonly IDisponibilidadService _disponibilidadService;

    public DisponibilidadController(IDisponibilidadService disponibilidadService)
    {
        _disponibilidadService = disponibilidadService;
    }

    /// <summary>Mis bloques de horario. Solo psicólogos.</summary>
    [Authorize(Roles = Roles.Psicologo)]
    [HttpGet("mia")]
    public async Task<ActionResult<List<DisponibilidadDto>>> ListarMia()
    {
        return Ok(await _disponibilidadService.ListarDePsicologoAsync(IdDelToken()));
    }

    [Authorize(Roles = Roles.Psicologo)]
    [HttpPost]
    public async Task<ActionResult<DisponibilidadDto>> Crear(DisponibilidadGuardarDto datos)
    {
        var bloque = await _disponibilidadService.CrearAsync(IdDelToken(), datos);
        return StatusCode(StatusCodes.Status201Created, bloque);
    }

    [Authorize(Roles = Roles.Psicologo)]
    [HttpPut("{id:int}")]
    public async Task<ActionResult<DisponibilidadDto>> Actualizar(int id, DisponibilidadGuardarDto datos)
    {
        return Ok(await _disponibilidadService.ActualizarAsync(id, IdDelToken(), datos));
    }

    [Authorize(Roles = Roles.Psicologo)]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Eliminar(int id)
    {
        await _disponibilidadService.EliminarAsync(id, IdDelToken());
        return NoContent();
    }

    /// <summary>Los bloques de un psicólogo, para el administrador.</summary>
    [Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Admin}")]
    [HttpGet("psicologo/{psicologoId:int}")]
    public async Task<ActionResult<List<DisponibilidadDto>>> ListarDePsicologo(int psicologoId)
    {
        return Ok(await _disponibilidadService.ListarDePsicologoAsync(psicologoId));
    }

    private int IdDelToken() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
