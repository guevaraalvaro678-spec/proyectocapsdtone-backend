using CapstonePsicologiaAPI.DTOs;
using CapstonePsicologiaAPI.Models;
using CapstonePsicologiaAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CapstonePsicologiaAPI.Controllers;

/// <summary>Donaciones de artículos físicos.</summary>
[ApiController]
[Route("api/donacion")]
public class DonacionController : ControllerBase
{
    private const string RolesAdmin = $"{Roles.SuperAdmin},{Roles.Admin}";

    private readonly IDonacionService _donacionService;

    public DonacionController(IDonacionService donacionService)
    {
        _donacionService = donacionService;
    }

    /// <summary>Registro público ("Registra tu donación"). No pide cuenta.</summary>
    [HttpPost]
    public async Task<ActionResult<DonacionDto>> Registrar([FromBody] DonacionCrearDto datos) =>
        StatusCode(StatusCodes.Status201Created, await _donacionService.RegistrarAsync(datos));

    [Authorize(Roles = RolesAdmin)]
    [HttpGet]
    public async Task<ActionResult<List<DonacionDto>>> Listar([FromQuery] string? estado = null, [FromQuery] string? buscar = null) =>
        Ok(await _donacionService.ListarAsync(estado, buscar));

    [Authorize(Roles = RolesAdmin)]
    [HttpPut("{id:int}/estado")]
    public async Task<ActionResult<DonacionDto>> CambiarEstado(int id, [FromBody] DonacionEstadoDto datos) =>
        Ok(await _donacionService.CambiarEstadoAsync(id, datos));

    [Authorize(Roles = RolesAdmin)]
    [HttpPut("{id:int}/observaciones")]
    public async Task<ActionResult<DonacionDto>> GuardarObservaciones(int id, [FromBody] DonacionObservacionesDto datos) =>
        Ok(await _donacionService.GuardarObservacionesAsync(id, datos.Observaciones));

    /// <summary>Totales por estado e inventario de lo recibido.</summary>
    [Authorize(Roles = RolesAdmin)]
    [HttpGet("resumen")]
    public async Task<ActionResult<DonacionResumenDto>> Resumen() =>
        Ok(await _donacionService.ResumenAsync());
}

/// <summary>Noticias del sitio público.</summary>
[ApiController]
[Route("api/noticia")]
public class NoticiaController : ControllerBase
{
    private const string RolesAdmin = $"{Roles.SuperAdmin},{Roles.Admin}";
    private const int MaximoPublicas = 6;

    private readonly INoticiaService _noticiaService;

    public NoticiaController(INoticiaService noticiaService)
    {
        _noticiaService = noticiaService;
    }

    /// <summary>Últimas noticias publicadas, para el sitio público.</summary>
    [HttpGet("publicas")]
    public async Task<ActionResult<List<NoticiaDto>>> Publicas() =>
        Ok(await _noticiaService.ListarPublicadasAsync(MaximoPublicas));

    [Authorize(Roles = RolesAdmin)]
    [HttpGet]
    public async Task<ActionResult<List<NoticiaDto>>> Listar() =>
        Ok(await _noticiaService.ListarTodasAsync());

    [Authorize(Roles = RolesAdmin)]
    [HttpPost]
    public async Task<ActionResult<NoticiaDto>> Crear([FromBody] NoticiaGuardarDto datos) =>
        StatusCode(StatusCodes.Status201Created, await _noticiaService.CrearAsync(datos));

    [Authorize(Roles = RolesAdmin)]
    [HttpPut("{id:int}")]
    public async Task<ActionResult<NoticiaDto>> Actualizar(int id, [FromBody] NoticiaGuardarDto datos) =>
        Ok(await _noticiaService.ActualizarAsync(id, datos));

    [Authorize(Roles = RolesAdmin)]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Eliminar(int id)
    {
        await _noticiaService.EliminarAsync(id);
        return NoContent();
    }
}
