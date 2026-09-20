using CapstonePsicologiaAPI.DTOs;

namespace CapstonePsicologiaAPI.Services;

public interface IContactoService
{
    /// <summary>Guarda un mensaje de contacto y devuelve lo que quedó registrado.</summary>
    Task<ContactoDto> CrearAsync(ContactoCrearDto contacto);

    /// <summary>Lista los mensajes recibidos, del más reciente al más antiguo.</summary>
    Task<List<ContactoDto>> ListarAsync();

    /// <summary>Marca un mensaje como leído; devuelve null si no existe.</summary>
    Task<ContactoDto?> MarcarLeidoAsync(int id);

    /// <summary>Responde por correo a quien escribió y marca el mensaje como leído. Devuelve null si no existe.</summary>
    Task<ContactoDto?> ResponderAsync(int id, string respuesta, int administradorId);
}
