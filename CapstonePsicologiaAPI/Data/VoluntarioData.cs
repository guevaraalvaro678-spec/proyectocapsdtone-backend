using CapstonePsicologiaAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace CapstonePsicologiaAPI.Data;

public interface IVoluntarioData
{
    Task<bool> ExisteDniAsync(string dni);
    Task CrearAsync(Voluntario voluntario);
    Task<Voluntario?> ObtenerPorUsuarioIdAsync(int usuarioId);
    Task<List<Voluntario>> ListarAsync(string? buscar);
}

public class VoluntarioData : IVoluntarioData
{
    private readonly AppDbContext _db;

    public VoluntarioData(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>El DNI es único entre todos los perfiles: una persona no puede registrarse dos veces.</summary>
    public async Task<bool> ExisteDniAsync(string dni) =>
        await _db.Voluntarios.AnyAsync(v => v.Dni == dni)
        || await _db.Practicantes.AnyAsync(p => p.Dni == dni)
        || await _db.Psicologos.AnyAsync(p => p.Dni == dni);

    public async Task CrearAsync(Voluntario voluntario)
    {
        _db.Voluntarios.Add(voluntario);
        await _db.SaveChangesAsync();
    }

    public Task<Voluntario?> ObtenerPorUsuarioIdAsync(int usuarioId) =>
        _db.Voluntarios.Include(v => v.Usuario).FirstOrDefaultAsync(v => v.UsuarioId == usuarioId);

    public Task<List<Voluntario>> ListarAsync(string? buscar)
    {
        var consulta = _db.Voluntarios.AsNoTracking().Include(v => v.Usuario).AsQueryable();

        if (!string.IsNullOrWhiteSpace(buscar))
        {
            var texto = buscar.Trim();
            consulta = consulta.Where(v => v.Dni.Contains(texto) || v.Apellidos.Contains(texto)
                                           || v.Usuario!.Nombre.Contains(texto) || v.Usuario.Correo!.Contains(texto));
        }

        return consulta.OrderByDescending(v => v.FechaRegistro).ToListAsync();
    }
}
