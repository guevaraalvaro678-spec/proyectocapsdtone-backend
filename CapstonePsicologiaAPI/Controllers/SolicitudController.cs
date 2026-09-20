using System.Security.Claims;
using CapstonePsicologiaAPI.DTOs;
using CapstonePsicologiaAPI.Models;
using CapstonePsicologiaAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CapstonePsicologiaAPI.Controllers;

[ApiController]
[Route("api/solicitud")]
public class SolicitudController : ControllerBase
{
    private const string RolesAdmin = $"{Roles.SuperAdmin},{Roles.Admin}";

    private readonly ISolicitudService _solicitudService;

    public SolicitudController(ISolicitudService solicitudService)
    {
        _solicitudService = solicitudService;
    }

    /// <summary>El usuario pide orientación. Solo puede tener una solicitud abierta a la vez.</summary>
    [Authorize(Roles = Roles.Usuario)]
    [HttpPost]
    public async Task<ActionResult<SolicitudDto>> Crear(SolicitudCrearDto datos)
    {
        var solicitud = await _solicitudService.CrearAsync(IdDelToken(), datos);

        if (solicitud is null)
            return Conflict(new { mensaje = "Ya tienes una solicitud en curso" });

        return StatusCode(StatusCodes.Status201Created, solicitud);
    }

    /// <summary>La solicitud más reciente del usuario logueado (204 si nunca pidió orientación).</summary>
    [Authorize(Roles = Roles.Usuario)]
    [HttpGet("mia")]
    public async Task<ActionResult<SolicitudDto>> ObtenerMia()
    {
        var solicitud = await _solicitudService.ObtenerMiaAsync(IdDelToken());
        return solicitud is null ? NoContent() : Ok(solicitud);
    }

    /// <summary>Todas las solicitudes. Solo administradores.</summary>
    [Authorize(Roles = RolesAdmin)]
    [HttpGet]
    public async Task<ActionResult<List<SolicitudDto>>> Listar()
    {
        return Ok(await _solicitudService.ListarAsync());
    }

    /// <summary>Solicitudes abiertas que un administrador le asignó al psicólogo que consulta.</summary>
    [Authorize(Roles = Roles.Psicologo)]
    [HttpGet("asignadas")]
    public async Task<ActionResult<List<SolicitudDto>>> ListarAsignadas()
    {
        return Ok(await _solicitudService.ListarAsignadasAsync(IdDelToken()));
    }

    /// <summary>Cambia el estado de una solicitud (y asigna psicólogo). Solo administradores.</summary>
    [Authorize(Roles = RolesAdmin)]
    [HttpPut("{id:int}/estado")]
    public async Task<ActionResult<SolicitudDto>> CambiarEstado(int id, SolicitudEstadoDto datos)
    {
        var solicitud = await _solicitudService.CambiarEstadoAsync(id, datos);
        return solicitud is null ? NotFound(new { mensaje = "Solicitud no encontrada" }) : Ok(solicitud);
    }

    private int IdDelToken() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
