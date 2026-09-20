using CapstonePsicologiaAPI.Data;
using CapstonePsicologiaAPI.DTOs;
using CapstonePsicologiaAPI.Models;

namespace CapstonePsicologiaAPI.Services;

public class ContactoService : IContactoService
{
    private readonly IContactoData _contactoData;
    private readonly ICorreoService _correo;

    public ContactoService(IContactoData contactoData, ICorreoService correo)
    {
        _contactoData = contactoData;
        _correo = correo;
    }

    public async Task<ContactoDto?> ResponderAsync(int id, string respuesta, int administradorId)
    {
        // Responder también deja el mensaje como leído.
        var contacto = await _contactoData.MarcarLeidoAsync(id);

        if (contacto is null)
            return null;

        await _correo.EncolarAsync(
            contacto.Correo,
            contacto.Nombre,
            "Respuesta a tu mensaje",
            PlantillasCorreo.RespuestaContacto(contacto.Nombre, respuesta.Trim(), contacto.Mensaje),
            TiposCorreo.RespuestaContacto,
            administradorId,
            propagarErrores: true);

        return ADto(contacto);
    }

    public async Task<ContactoDto> CrearAsync(ContactoCrearDto contacto)
    {
        var entidad = new Contacto
        {
            Nombre = contacto.Nombre.Trim(),
            Correo = contacto.Correo.Trim().ToLowerInvariant(),
            Mensaje = contacto.Mensaje.Trim(),
            FechaEnvio = DateTime.UtcNow,
        };

        await _contactoData.CrearAsync(entidad);

        return ADto(entidad);
    }

    public async Task<List<ContactoDto>> ListarAsync()
    {
        var contactos = await _contactoData.ObtenerTodosAsync();
        return contactos.Select(ADto).ToList();
    }

    public async Task<ContactoDto?> MarcarLeidoAsync(int id)
    {
        var contacto = await _contactoData.MarcarLeidoAsync(id);
        return contacto is null ? null : ADto(contacto);
    }

    private static ContactoDto ADto(Contacto c) => new()
    {
        Id = c.Id,
        Nombre = c.Nombre,
        Correo = c.Correo,
        Mensaje = c.Mensaje,
        // SQL Server devuelve la fecha sin zona; se marca como UTC para que el front la convierta bien.
        FechaEnvio = DateTime.SpecifyKind(c.FechaEnvio, DateTimeKind.Utc),
        Leido = c.Leido,
    };
}
