using System.Security.Claims;
using CapstonePsicologiaAPI.DTOs;
using CapstonePsicologiaAPI.Models;
using CapstonePsicologiaAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CapstonePsicologiaAPI.Controllers;

[ApiController]
[Route("api/colaborador")]
public class ColaboradorController : ControllerBase
{
    private const string RolesAdmin = $"{Roles.SuperAdmin},{Roles.Admin}";

    private readonly IColaboradorService _colaboradorService;

    public ColaboradorController(IColaboradorService colaboradorService)
    {
        _colaboradorService = colaboradorService;
    }

    /// <summary>Postulación pública de un psicólogo colaborador. No crea cuenta hasta que el administrador aprueba.</summary>
    [HttpPost]
    public async Task<IActionResult> Postular([FromBody] ColaboradorPostulacionDto datos)
    {
        await _colaboradorService.PostularAsync(datos);
        return Ok(new { mensaje = "Postulación enviada" });
    }

    [Authorize(Roles = RolesAdmin)]
    [HttpGet]
    public async Task<ActionResult<List<ColaboradorDto>>> Listar([FromQuery] string? estado = null) =>
        Ok(await _colaboradorService.ListarAsync(estado));

    [Authorize(Roles = RolesAdmin)]
    [HttpPut("{id:int}/aprobar")]
    public async Task<ActionResult<ColaboradorDto>> Aprobar(int id) =>
        Ok(await _colaboradorService.AprobarAsync(id, AdministradorId()));

    [Authorize(Roles = RolesAdmin)]
    [HttpPut("{id:int}/rechazar")]
    public async Task<ActionResult<ColaboradorDto>> Rechazar(int id, [FromBody] ColaboradorRechazoDto datos) =>
        Ok(await _colaboradorService.RechazarAsync(id, datos.Motivo, AdministradorId()));

    private int AdministradorId() =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
}
