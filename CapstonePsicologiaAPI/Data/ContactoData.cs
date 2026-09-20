using CapstonePsicologiaAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace CapstonePsicologiaAPI.Data;

/// <summary>Acceso a datos de los mensajes de contacto (tabla Contactos).</summary>
public class ContactoData : IContactoData
{
    private readonly AppDbContext _db;

    public ContactoData(AppDbContext db)
    {
        _db = db;
    }

    public async Task CrearAsync(Contacto contacto)
    {
        _db.Contactos.Add(contacto);
        await _db.SaveChangesAsync();
    }

    public Task<List<Contacto>> ObtenerTodosAsync()
    {
        return _db.Contactos
            .AsNoTracking()
            .OrderByDescending(c => c.FechaEnvio)
            .ToListAsync();
    }

    public async Task<Contacto?> MarcarLeidoAsync(int id)
    {
        var contacto = await _db.Contactos.FirstOrDefaultAsync(c => c.Id == id);

        if (contacto is null)
            return null;

        contacto.Leido = true;
        await _db.SaveChangesAsync();
        return contacto;
    }
}
