using CapstonePsicologiaAPI.DTOs;
using CapstonePsicologiaAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace CapstonePsicologiaAPI.Controllers;

/// <summary>Apoyo para "Iniciar / registrarse con Google". El login en sí es POST api/admin/google.</summary>
[ApiController]
[Route("api/google")]
public class GoogleController : ControllerBase
{
    private readonly IGoogleAuthService _google;

    public GoogleController(IGoogleAuthService google)
    {
        _google = google;
    }

    [HttpGet("config")]
    public ActionResult<GoogleConfigDto> Config() => Ok(new GoogleConfigDto { ClientId = _google.ClientId });

    /// <summary>Valida el token y devuelve nombre y correo, para precargar el formulario de registro.</summary>
    [HttpPost("perfil")]
    public async Task<ActionResult<GooglePerfilDto>> Perfil([FromBody] GoogleCredencialDto datos) =>
        Ok(await _google.ValidarAsync(datos.Credential));
}
