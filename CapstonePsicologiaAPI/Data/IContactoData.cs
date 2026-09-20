using CapstonePsicologiaAPI.Models;

namespace CapstonePsicologiaAPI.Data;

public interface IContactoData
{
    Task CrearAsync(Contacto contacto);

    /// <summary>Todos los mensajes, del más reciente al más antiguo.</summary>
    Task<List<Contacto>> ObtenerTodosAsync();

    /// <summary>Marca un mensaje como leído; devuelve null si no existe.</summary>
    Task<Contacto?> MarcarLeidoAsync(int id);
}
