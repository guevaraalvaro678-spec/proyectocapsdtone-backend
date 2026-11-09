using CapstonePsicologiaAPI.DTOs;
using CapstonePsicologiaAPI.Models;
using CapstonePsicologiaAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CapstonePsicologiaAPI.Controllers;

[ApiController]
[Authorize]
[Route("api/dashboard")]
public class DashboardController : ControllerBase
{
    private const string Administracion = $"{Roles.SuperAdmin},{Roles.Admin}";

    private readonly IDashboardService _dashboardService;
    private readonly IDashboardOngService _dashboardOngService;

    public DashboardController(IDashboardService dashboardService, IDashboardOngService dashboardOngService)
    {
        _dashboardService = dashboardService;
        _dashboardOngService = dashboardOngService;
    }

    /// <summary>Resumen del módulo de psicología anterior (el front ya no lo usa).</summary>
    [Authorize(Roles = Administracion)]
    [HttpGet]
    public async Task<ActionResult<DashboardDto>> Obtener()
    {
        return Ok(await _dashboardService.ObtenerAsync());
    }

    /// <summary>Indicadores de la ONG: selección, miembros, donaciones y próximas entrevistas.</summary>
    [Authorize(Roles = Administracion)]
    [HttpGet("ong")]
    public async Task<ActionResult<DashboardOngDto>> ObtenerOng() =>
        Ok(await _dashboardOngService.ObtenerAsync());

    [Authorize(Roles = $"{Administracion},{Roles.RRHH}")]
    [HttpGet("indicadores")]
    public async Task<ActionResult<IndicadoresProcesoDto>> Indicadores() =>
        Ok(await _dashboardOngService.ObtenerIndicadoresAsync());

    /// <summary>Bandeja de trabajo de Recursos Humanos: postulaciones, entrevistas, horarios y actividades que esperan una acción.</summary>
    [Authorize(Roles = $"{Administracion},{Roles.RRHH}")]
    [HttpGet("bandeja")]
    public async Task<ActionResult<BandejaRrhhDto>> Bandeja() =>
        Ok(await _dashboardOngService.ObtenerBandejaAsync());
}
