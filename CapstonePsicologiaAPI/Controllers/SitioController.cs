using System.Security.Claims;
using CapstonePsicologiaAPI.DTOs;
using CapstonePsicologiaAPI.Models;
using CapstonePsicologiaAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CapstonePsicologiaAPI.Controllers;

/// <summary>Contenido editable del sitio público: imágenes, logo e iniciativas.</summary>
[ApiController]
public class SitioController : ControllerBase
{
    private const string RolesAdmin = $"{Roles.SuperAdmin},{Roles.Admin}";

    private readonly ISitioService _sitioService;

    public SitioController(ISitioService sitioService)
    {
        _sitioService = sitioService;
    }

    /// <summary>Logo e iniciativas activas, para el sitio público.</summary>
    [HttpGet("api/sitio")]
    public async Task<ActionResult<SitioPublicoDto>> Publico() =>
        Ok(await _sitioService.ObtenerPublicoAsync());

    /// <summary>Sirve una imagen subida. Pública, porque la ven los visitantes.</summary>
    [HttpGet("api/imagen/{id:int}")]
    public async Task<IActionResult> Imagen(int id)
    {
        var imagen = await _sitioService.ObtenerImagenAsync(id);
        Response.Headers.CacheControl = "public, max-age=3600";
        return File(imagen.Datos, imagen.TipoContenido);
    }

    [Authorize(Roles = RolesAdmin)]
    [HttpPost("api/imagen")]
    [RequestSizeLimit(4 * 1024 * 1024)]
    public async Task<ActionResult<ImagenSubidaDto>> Subir(IFormFile archivo) =>
        StatusCode(StatusCodes.Status201Created, await _sitioService.SubirImagenAsync(archivo));

    [Authorize(Roles = RolesAdmin)]
    [HttpPut("api/sitio/logo")]
    public async Task<IActionResult> GuardarLogo([FromBody] LogoGuardarDto datos)
    {
        await _sitioService.GuardarLogoAsync(datos.ImagenId);
        return NoContent();
    }

    [Authorize(Roles = RolesAdmin)]
    [HttpGet("api/iniciativa")]
    public async Task<ActionResult<List<IniciativaDto>>> Listar() =>
        Ok(await _sitioService.ListarIniciativasAsync());

    [Authorize(Roles = RolesAdmin)]
    [HttpPost("api/iniciativa")]
    public async Task<ActionResult<IniciativaDto>> Crear([FromBody] IniciativaGuardarDto datos) =>
        StatusCode(StatusCodes.Status201Created, await _sitioService.CrearIniciativaAsync(datos));

    [Authorize(Roles = RolesAdmin)]
    [HttpPut("api/iniciativa/{id:int}")]
    public async Task<ActionResult<IniciativaDto>> Actualizar(int id, [FromBody] IniciativaGuardarDto datos) =>
        Ok(await _sitioService.ActualizarIniciativaAsync(id, datos));

    [Authorize(Roles = RolesAdmin)]
    [HttpDelete("api/iniciativa/{id:int}")]
    public async Task<IActionResult> Eliminar(int id)
    {
        await _sitioService.EliminarIniciativaAsync(id);
        return NoContent();
    }

    /// <summary>Iniciativas que propuso el voluntario o practicante que inició sesión.</summary>
    [Authorize(Roles = $"{Roles.Voluntario},{Roles.Practicante}")]
    [HttpGet("api/iniciativa/mias")]
    public async Task<ActionResult<List<IniciativaDto>>> Mias() =>
        Ok(await _sitioService.ListarMiasAsync(UsuarioId()));

    [Authorize(Roles = $"{Roles.Voluntario},{Roles.Practicante}")]
    [HttpPost("api/iniciativa/propuesta")]
    public async Task<ActionResult<IniciativaDto>> Proponer([FromBody] IniciativaPropuestaDto datos) =>
        StatusCode(StatusCodes.Status201Created, await _sitioService.ProponerAsync(UsuarioId(), datos));

    [Authorize(Roles = RolesAdmin)]
    [HttpPut("api/iniciativa/{id:int}/revision")]
    public async Task<ActionResult<IniciativaDto>> Revisar(int id, [FromBody] IniciativaRevisionDto datos) =>
        Ok(await _sitioService.RevisarAsync(id, datos));

    private int UsuarioId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
