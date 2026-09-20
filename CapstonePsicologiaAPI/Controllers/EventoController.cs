using CapstonePsicologiaAPI.DTOs;
using CapstonePsicologiaAPI.Models;
using CapstonePsicologiaAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CapstonePsicologiaAPI.Controllers;

[ApiController]
[Route("api/evento")]
public class EventoController : ControllerBase
{
    private const string RolesAdmin = $"{Roles.SuperAdmin},{Roles.Admin}";

    private readonly IEventoService _eventoService;

    public EventoController(IEventoService eventoService)
    {
        _eventoService = eventoService;
    }

    /// <summary>Público: los próximos eventos publicados, para el banner del sitio web.</summary>
    [HttpGet]
    public async Task<ActionResult<List<EventoDto>>> ListarPublicos()
    {
        return Ok(await _eventoService.ListarPublicosAsync());
    }

    /// <summary>Todos los eventos, publicados o no. Solo administradores.</summary>
    [Authorize(Roles = RolesAdmin)]
    [HttpGet("todos")]
    public async Task<ActionResult<List<EventoDto>>> ListarTodos()
    {
        return Ok(await _eventoService.ListarTodosAsync());
    }

    [Authorize(Roles = RolesAdmin)]
    [HttpPost]
    public async Task<ActionResult<EventoDto>> Crear(EventoGuardarDto datos)
    {
        var evento = await _eventoService.CrearAsync(datos);
        return StatusCode(StatusCodes.Status201Created, evento);
    }

    [Authorize(Roles = RolesAdmin)]
    [HttpPut("{id:int}")]
    public async Task<ActionResult<EventoDto>> Actualizar(int id, EventoGuardarDto datos)
    {
        var evento = await _eventoService.ActualizarAsync(id, datos);
        return evento is null ? NotFound(new { mensaje = "Evento no encontrado" }) : Ok(evento);
    }

    [Authorize(Roles = RolesAdmin)]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Eliminar(int id)
    {
        var eliminado = await _eventoService.EliminarAsync(id);
        return eliminado ? NoContent() : NotFound(new { mensaje = "Evento no encontrado" });
    }
}
