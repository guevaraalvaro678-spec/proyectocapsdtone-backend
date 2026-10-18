using System.Security.Claims;
using CapstonePsicologiaAPI.DTOs;
using CapstonePsicologiaAPI.Models;
using CapstonePsicologiaAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CapstonePsicologiaAPI.Controllers;

/// <summary>Postulaciones (proceso de selección) y el CV adjunto.</summary>
[ApiController]
[Authorize]
public class PostulacionController : ControllerBase
{
    private const string RolesGestion = $"{Roles.SuperAdmin},{Roles.Admin},{Roles.RRHH}";

    private readonly IPostulacionService _postulacionService;

    public PostulacionController(IPostulacionService postulacionService)
    {
        _postulacionService = postulacionService;
    }

    /// <summary>Sube el CV en PDF (máx. 10 MB). Devuelve el Id para enviarlo con la postulación.</summary>
    [Authorize(Roles = Roles.Postulante)]
    [HttpPost("api/archivo/cv")]
    [RequestSizeLimit(12 * 1024 * 1024)]
    public async Task<ActionResult<ArchivoSubidoDto>> SubirCv(IFormFile archivo) =>
        StatusCode(StatusCodes.Status201Created, await _postulacionService.SubirCvAsync(archivo, UsuarioId()));

    /// <summary>Descarga un archivo: el equipo de gestión o quien lo subió.</summary>
    [HttpGet("api/archivo/{id:int}")]
    public async Task<IActionResult> Descargar(int id)
    {
        var archivo = await _postulacionService.ObtenerArchivoAsync(id, UsuarioId(), Rol());
        return File(archivo.Datos, archivo.TipoContenido, archivo.Nombre);
    }

    [Authorize(Roles = Roles.Postulante)]
    [HttpPost("api/postulacion")]
    public async Task<ActionResult<PostulacionDto>> Crear([FromBody] PostulacionCrearDto datos) =>
        StatusCode(StatusCodes.Status201Created, await _postulacionService.CrearAsync(UsuarioId(), datos));

    /// <summary>La postulación de la cuenta que inició sesión (null si todavía no postuló).</summary>
    [HttpGet("api/postulacion/mia")]
    public async Task<ActionResult<PostulacionDto?>> Mia() =>
        Ok(await _postulacionService.ObtenerMiaAsync(UsuarioId()));

    [Authorize(Roles = RolesGestion)]
    [HttpGet("api/postulacion")]
    public async Task<ActionResult<List<PostulacionDto>>> Listar(
        [FromQuery] string? tipo = null, [FromQuery] string? estado = null, [FromQuery] string? buscar = null) =>
        Ok(await _postulacionService.ListarAsync(tipo, estado, buscar));

    [Authorize(Roles = RolesGestion)]
    [HttpGet("api/postulacion/{id:int}")]
    public async Task<ActionResult<PostulacionDto>> Obtener(int id) =>
        Ok(await _postulacionService.ObtenerAsync(id));

    [Authorize(Roles = RolesGestion)]
    [HttpPut("api/postulacion/{id:int}/estado")]
    public async Task<ActionResult<PostulacionDto>> CambiarEstado(int id, [FromBody] PostulacionEstadoDto datos) =>
        Ok(await _postulacionService.CambiarEstadoAsync(id, datos));

    private int UsuarioId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private string Rol() => User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
}

/// <summary>Sesiones de entrevista grupal.</summary>
[ApiController]
[Route("api/entrevista")]
[Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Admin},{Roles.RRHH}")]
public class EntrevistaController : ControllerBase
{
    private readonly ISesionEntrevistaService _sesionService;

    public EntrevistaController(ISesionEntrevistaService sesionService)
    {
        _sesionService = sesionService;
    }

    [HttpGet]
    public async Task<ActionResult<List<SesionEntrevistaDto>>> Listar([FromQuery] string? estado = null) =>
        Ok(await _sesionService.ListarAsync(estado));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<SesionEntrevistaDetalleDto>> Obtener(int id) =>
        Ok(await _sesionService.ObtenerDetalleAsync(id));

    [HttpPost]
    public async Task<ActionResult<SesionEntrevistaDto>> Crear([FromBody] SesionEntrevistaGuardarDto datos) =>
        StatusCode(StatusCodes.Status201Created, await _sesionService.CrearAsync(datos));

    [HttpPut("{id:int}")]
    public async Task<ActionResult<SesionEntrevistaDto>> Actualizar(int id, [FromBody] SesionEntrevistaGuardarDto datos) =>
        Ok(await _sesionService.ActualizarAsync(id, datos));

    /// <summary>Realizada pasa a sus postulantes a "Entrevista realizada"; Cancelada los devuelve a "Habilitada".</summary>
    [HttpPut("{id:int}/estado")]
    public async Task<ActionResult<SesionEntrevistaDto>> CambiarEstado(int id, [FromBody] SesionEstadoDto datos) =>
        Ok(await _sesionService.CambiarEstadoAsync(id, datos.Estado));
}
