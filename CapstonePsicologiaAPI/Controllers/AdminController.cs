using CapstonePsicologiaAPI.DTOs;
using CapstonePsicologiaAPI.Models;
using CapstonePsicologiaAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CapstonePsicologiaAPI.Controllers;

[ApiController]
[Route("api/admin")]
public class AdminController : ControllerBase
{
    private readonly IAdminService _adminService;

    public AdminController(IAdminService adminService)
    {
        _adminService = adminService;
    }

    [HttpPost("login")]
    public async Task<ActionResult<AdminSesionDto>> Login(AdminLoginDto login)
    {
        var sesion = await _adminService.LoginAsync(login);

        if (sesion is null)
            return Unauthorized(new { mensaje = "Usuario o contraseña incorrectos" });

        return Ok(sesion);
    }

    /// <summary>Solo un SuperAdmin logueado puede crear nuevos usuarios.</summary>
    [Authorize(Roles = Roles.SuperAdmin)]
    [HttpPost("registro")]
    public async Task<ActionResult<AdminDto>> Registro(AdminRegistroDto registro)
    {
        var admin = await _adminService.RegistrarAsync(registro);

        if (admin is null)
            return Conflict(new { mensaje = "Ese nombre de usuario ya existe" });

        return StatusCode(StatusCodes.Status201Created, admin);
    }
}
