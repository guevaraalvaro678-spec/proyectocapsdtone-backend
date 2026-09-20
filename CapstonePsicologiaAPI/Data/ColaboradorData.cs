using CapstonePsicologiaAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace CapstonePsicologiaAPI.Data;

public class ColaboradorData : IColaboradorData
{
    private readonly AppDbContext _db;

    public ColaboradorData(AppDbContext db)
    {
        _db = db;
    }

    public Task<Psicologo?> ObtenerPorIdAsync(int id) =>
        _db.Psicologos.FirstOrDefaultAsync(p => p.Id == id);

    public Task<Psicologo?> ObtenerPorCorreoAsync(string correo) =>
        _db.Psicologos.FirstOrDefaultAsync(p => p.Correo == correo);

    public Task<Psicologo?> ObtenerPorUsuarioIdAsync(int usuarioId) =>
        _db.Psicologos.FirstOrDefaultAsync(p => p.UsuarioId == usuarioId);

    public Task<List<Psicologo>> ListarAsync(string? estado)
    {
        var consulta = _db.Psicologos.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(estado))
            consulta = consulta.Where(p => p.Estado == estado);

        return consulta.OrderBy(p => p.Estado == EstadosColaborador.Pendiente ? 0 : 1)
            .ThenByDescending(p => p.FechaSolicitud)
            .ToListAsync();
    }

    public async Task CrearAsync(Psicologo psicologo)
    {
        _db.Psicologos.Add(psicologo);
        await _db.SaveChangesAsync();
    }

    public Task GuardarAsync() => _db.SaveChangesAsync();
}
