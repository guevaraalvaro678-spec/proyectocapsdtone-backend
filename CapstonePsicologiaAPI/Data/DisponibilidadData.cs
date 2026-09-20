using CapstonePsicologiaAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace CapstonePsicologiaAPI.Data;

/// <summary>Acceso a datos de los horarios que deja disponibles cada psicólogo (tabla Disponibilidades).</summary>
public class DisponibilidadData : IDisponibilidadData
{
    private readonly AppDbContext _db;

    public DisponibilidadData(AppDbContext db)
    {
        _db = db;
    }

    public Task<List<Disponibilidad>> ListarDePsicologoAsync(int psicologoId, bool soloActivos)
    {
        var consulta = _db.Disponibilidades.AsNoTracking().Where(d => d.PsicologoId == psicologoId);

        if (soloActivos)
            consulta = consulta.Where(d => d.Activo);

        return consulta.OrderBy(d => d.DiaSemana).ThenBy(d => d.HoraInicio).ToListAsync();
    }

    public Task<Disponibilidad?> ObtenerPorIdAsync(int id)
    {
        return _db.Disponibilidades.FirstOrDefaultAsync(d => d.Id == id);
    }

    public async Task CrearAsync(Disponibilidad disponibilidad)
    {
        _db.Disponibilidades.Add(disponibilidad);
        await _db.SaveChangesAsync();
    }

    public async Task ActualizarAsync(Disponibilidad disponibilidad)
    {
        _db.Disponibilidades.Update(disponibilidad);
        await _db.SaveChangesAsync();
    }

    public async Task EliminarAsync(Disponibilidad disponibilidad)
    {
        _db.Disponibilidades.Remove(disponibilidad);
        await _db.SaveChangesAsync();
    }

    public async Task<HashSet<int>> ObtenerPsicologosConHorarioAsync()
    {
        var ids = await _db.Disponibilidades
            .AsNoTracking()
            .Where(d => d.Activo)
            .Select(d => d.PsicologoId)
            .Distinct()
            .ToListAsync();

        return ids.ToHashSet();
    }
}
