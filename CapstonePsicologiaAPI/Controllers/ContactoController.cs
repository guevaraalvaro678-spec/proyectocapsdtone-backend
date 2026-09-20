using System.Security.Claims;
using CapstonePsicologiaAPI.DTOs;
using CapstonePsicologiaAPI.Models;
using CapstonePsicologiaAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CapstonePsicologiaAPI.Controllers;

[ApiController]
[Route("api/contacto")]
public class ContactoController : ControllerBase
{
    private readonly IContactoService _contactoService;

    public ContactoController(IContactoService contactoService)
    {
        _contactoService = contactoService;
    }

    /// <summary>Endpoint público: lo usa el formulario de contacto del sitio web (no requiere login).</summary>
    [HttpPost]
    public async Task<ActionResult<ContactoDto>> Crear(ContactoCrearDto contacto)
    {
        var creado = await _contactoService.CrearAsync(contacto);
        return StatusCode(StatusCodes.Status201Created, creado);
    }

    /// <summary>Lista los mensajes recibidos. Solo administradores con sesión.</summary>
    [Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Admin}")]
    [HttpGet]
    public async Task<ActionResult<List<ContactoDto>>> Listar()
    {
        return Ok(await _contactoService.ListarAsync());
    }

    /// <summary>Marca un mensaje como leído. Solo administradores con sesión.</summary>
    [Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Admin}")]
    [HttpPut("{id:int}/leido")]
    public async Task<ActionResult<ContactoDto>> MarcarLeido(int id)
    {
        var contacto = await _contactoService.MarcarLeidoAsync(id);

        if (contacto is null)
            return NotFound(new { mensaje = "Mensaje no encontrado" });

        return Ok(contacto);
    }

    /// <summary>Responde por correo a quien escribió. Solo administradores con sesión.</summary>
    [Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Admin}")]
    [HttpPost("{id:int}/responder")]
    public async Task<ActionResult<ContactoDto>> Responder(int id, ContactoResponderDto datos)
    {
        var adminId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var contacto = await _contactoService.ResponderAsync(id, datos.Respuesta, adminId);

        if (contacto is null)
            return NotFound(new { mensaje = "Mensaje no encontrado" });

        return Ok(contacto);
    }
}
