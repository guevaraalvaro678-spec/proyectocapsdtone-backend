using System.Security.Claims;
using CapstonePsicologiaAPI.DTOs;
using CapstonePsicologiaAPI.Models;
using CapstonePsicologiaAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CapstonePsicologiaAPI.Controllers;

[ApiController]
[Route("api/tarea")]
[Authorize]
public class TareaController : ControllerBase
{
    private const string RolesGestion = $"{Roles.SuperAdmin},{Roles.Admin},{Roles.RRHH}";
    private const string RolesParticipantes = $"{Roles.Voluntario},{Roles.Practicante}";

    private readonly ITareaService _tareaService;

    public TareaController(ITareaService tareaService)
    {
        _tareaService = tareaService;
    }

    [Authorize(Roles = RolesGestion)]
    [HttpPost]
    public async Task<ActionResult<TareaDto>> Crear([FromBody] TareaCrearDto datos) =>
        StatusCode(StatusCodes.Status201Created, await _tareaService.CrearAsync(datos, UsuarioId()));

    [Authorize(Roles = RolesGestion)]
    [HttpGet]
    public async Task<ActionResult<List<TareaDto>>> Listar([FromQuery] int? usuarioId = null) =>
        Ok(await _tareaService.ListarAsync(usuarioId));

    /// <summary>Las tareas del voluntario o practicante que inició sesión.</summary>
    [Authorize(Roles = RolesParticipantes)]
    [HttpGet("mias")]
    public async Task<ActionResult<List<TareaDto>>> Mias() =>
        Ok(await _tareaService.ListarAsync(UsuarioId()));

    [HttpPut("{id:int}/estado")]
    public async Task<ActionResult<TareaDto>> CambiarEstado(int id, [FromBody] TareaEstadoDto datos) =>
        Ok(await _tareaService.CambiarEstadoAsync(id, datos.Estado, UsuarioId(), User.FindFirstValue(ClaimTypes.Role)!));

    private int UsuarioId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
