using System.Security.Claims;
using CapstonePsicologiaAPI.DTOs;
using CapstonePsicologiaAPI.Models;
using CapstonePsicologiaAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CapstonePsicologiaAPI.Controllers;

[ApiController]
[Route("api/voluntario")]
public class VoluntarioController : ControllerBase
{
    private const string RolesGestion = $"{Roles.SuperAdmin},{Roles.Admin},{Roles.RRHH}";

    private readonly IVoluntarioService _voluntarioService;

    public VoluntarioController(IVoluntarioService voluntarioService)
    {
        _voluntarioService = voluntarioService;
    }

    /// <summary>Registro público. La contraseña temporal se envía por correo.</summary>
    [HttpPost("registro")]
    public async Task<IActionResult> Registrar([FromBody] VoluntarioRegistroDto datos)
    {
        // Con Google se devuelve la sesión (entra directo); si no, un mensaje: la clave llega por correo.
        var sesion = await _voluntarioService.RegistrarAsync(datos);

        if (sesion is not null)
            return StatusCode(StatusCodes.Status201Created, sesion);

        return StatusCode(StatusCodes.Status201Created, new { mensaje = "Registro exitoso. Revisa tu correo para ingresar." });
    }

    [Authorize(Roles = Roles.Voluntario)]
    [HttpGet("yo")]
    public async Task<ActionResult<VoluntarioDto>> Yo() =>
        Ok(await _voluntarioService.ObtenerYoAsync(int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!)));

    [Authorize(Roles = RolesGestion)]
    [HttpGet]
    public async Task<ActionResult<List<VoluntarioDto>>> Listar([FromQuery] string? buscar = null) =>
        Ok(await _voluntarioService.ListarAsync(buscar));
}
