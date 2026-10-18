using CapstonePsicologiaAPI.DTOs;
using CapstonePsicologiaAPI.Models;
using CapstonePsicologiaAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CapstonePsicologiaAPI.Controllers;

[ApiController]
[Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Admin}")]
[Route("api/dashboard")]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;
    private readonly IDashboardOngService _dashboardOngService;

    public DashboardController(IDashboardService dashboardService, IDashboardOngService dashboardOngService)
    {
        _dashboardService = dashboardService;
        _dashboardOngService = dashboardOngService;
    }

    /// <summary>Resumen del módulo de psicología anterior (el front ya no lo usa).</summary>
    [HttpGet]
    public async Task<ActionResult<DashboardDto>> Obtener()
    {
        return Ok(await _dashboardService.ObtenerAsync());
    }

    /// <summary>Indicadores de la ONG: selección, miembros, donaciones y próximas entrevistas.</summary>
    [HttpGet("ong")]
    public async Task<ActionResult<DashboardOngDto>> ObtenerOng() =>
        Ok(await _dashboardOngService.ObtenerAsync());
}
