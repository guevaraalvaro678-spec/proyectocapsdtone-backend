using CapstonePsicologiaAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace CapstonePsicologiaAPI.Data;

/// <summary>Acceso a datos de la bandeja de salida de correos (tabla Correos).</summary>
public class CorreoData : ICorreoData
{
    private readonly AppDbContext _db;

    public CorreoData(AppDbContext db)
    {
        _db = db;
    }

    public async Task CrearVariosAsync(IEnumerable<CorreoSaliente> correos)
    {
        _db.Correos.AddRange(correos);
        await _db.SaveChangesAsync();
    }

    public Task<List<CorreoSaliente>> ObtenerPendientesAsync(int maximo)
    {
        return _db.Correos
            .AsNoTracking()
            .Where(c => c.Estado == EstadosCorreo.Pendiente)
            .OrderBy(c => c.Id)
            .Take(maximo)
            .ToListAsync();
    }

    public async Task<bool> ReclamarAsync(int id)
    {
        var filas = await _db.Correos
            .Where(c => c.Id == id && c.Estado == EstadosCorreo.Pendiente)
            .ExecuteUpdateAsync(s => s.SetProperty(c => c.Estado, EstadosCorreo.Enviando));

        return filas == 1;
    }

    public async Task MarcarEnviadoAsync(int id)
    {
        await _db.Correos
            .Where(c => c.Id == id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(c => c.Estado, EstadosCorreo.Enviado)
                .SetProperty(c => c.FechaEnvio, DateTime.UtcNow)
                .SetProperty(c => c.Error, (string?)null));
    }

    public async Task MarcarFallidoAsync(int id, string error)
    {
        var mensaje = error.Length > 500 ? error[..500] : error;

        await _db.Correos
            .Where(c => c.Id == id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(c => c.Estado, EstadosCorreo.Fallido)
                .SetProperty(c => c.Error, mensaje));
    }

    public async Task<bool> ReintentarAsync(int id)
    {
        var filas = await _db.Correos
            .Where(c => c.Id == id && c.Estado == EstadosCorreo.Fallido)
            .ExecuteUpdateAsync(s => s
                .SetProperty(c => c.Estado, EstadosCorreo.Pendiente)
                .SetProperty(c => c.Error, (string?)null));

        return filas == 1;
    }

    public Task<List<CorreoSaliente>> ObtenerHistorialAsync(int maximo)
    {
        return _db.Correos
            .AsNoTracking()
            .OrderByDescending(c => c.Id)
            .Take(maximo)
            .ToListAsync();
    }

    public Task<int> ContarPendientesAsync()
    {
        return _db.Correos.CountAsync(c => c.Estado == EstadosCorreo.Pendiente);
    }
}
