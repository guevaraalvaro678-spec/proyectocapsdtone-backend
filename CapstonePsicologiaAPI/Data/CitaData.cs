using CapstonePsicologiaAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace CapstonePsicologiaAPI.Data;

/// <summary>Acceso a datos de las citas (tabla Citas).</summary>
public class CitaData : ICitaData
{
    /// <summary>La duración máxima permitida de una cita; sirve para acotar la búsqueda de cruces.</summary>
    private const int DuracionMaximaMinutos = 180;

    private readonly AppDbContext _db;

    public CitaData(AppDbContext db)
    {
        _db = db;
    }

    public async Task CrearAsync(Cita cita)
    {
        _db.Citas.Add(cita);
        await _db.SaveChangesAsync();
    }

    public Task<Cita?> ObtenerPorIdAsync(int id)
    {
        return _db.Citas
            .Include(c => c.Paciente)
            .Include(c => c.Psicologo)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task ActualizarAsync(Cita cita)
    {
        _db.Citas.Update(cita);
        await _db.SaveChangesAsync();
    }

    public Task<List<Cita>> ListarAsync(CitaFiltro filtro)
    {
        var consulta = _db.Citas
            .AsNoTracking()
            .Include(c => c.Paciente)
            .Include(c => c.Psicologo)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(filtro.Estado))
            consulta = consulta.Where(c => c.Estado == filtro.Estado);

        if (filtro.PsicologoId is not null)
            consulta = consulta.Where(c => c.PsicologoId == filtro.PsicologoId);

        if (filtro.PacienteId is not null)
            consulta = consulta.Where(c => c.PacienteId == filtro.PacienteId);

        if (filtro.Desde is not null)
            consulta = consulta.Where(c => c.FechaInicio >= filtro.Desde);

        if (filtro.Hasta is not null)
            consulta = consulta.Where(c => c.FechaInicio <= filtro.Hasta);

        consulta = consulta.OrderBy(c => c.FechaInicio);

        if (filtro.Maximo is not null)
            consulta = consulta.Take(filtro.Maximo.Value);

        return consulta.ToListAsync();
    }

    public async Task<bool> HaySolapamientoAsync(int? psicologoId, int? pacienteId, DateTime inicio, DateTime fin, int? excluirId)
    {
        var consulta = _db.Citas
            .AsNoTracking()
            .Where(c => c.Estado == EstadosCita.Programada || c.Estado == EstadosCita.Confirmada)
            .Where(c => c.FechaInicio < fin && c.FechaInicio > inicio.AddMinutes(-DuracionMaximaMinutos));

        if (psicologoId is not null)
            consulta = consulta.Where(c => c.PsicologoId == psicologoId);

        if (pacienteId is not null)
            consulta = consulta.Where(c => c.PacienteId == pacienteId);

        if (excluirId is not null)
            consulta = consulta.Where(c => c.Id != excluirId);

        // La comparación exacta con la duración de cada cita se hace en memoria, sobre pocas filas.
        var candidatas = await consulta.Select(c => new { c.FechaInicio, c.DuracionMinutos }).ToListAsync();
        return candidatas.Any(c => c.FechaInicio.AddMinutes(c.DuracionMinutos) > inicio);
    }

    public Task<List<Cita>> ObtenerParaRecordatorioAsync(DateTime desde, DateTime hasta)
    {
        return _db.Citas
            .AsNoTracking()
            .Include(c => c.Paciente)
            .Include(c => c.Psicologo)
            .Where(c => !c.RecordatorioEnviado)
            .Where(c => c.Estado == EstadosCita.Programada || c.Estado == EstadosCita.Confirmada)
            .Where(c => c.FechaInicio >= desde && c.FechaInicio <= hasta)
            .ToListAsync();
    }

    public async Task<bool> ReclamarRecordatorioAsync(int id)
    {
        var filas = await _db.Citas
            .Where(c => c.Id == id && !c.RecordatorioEnviado)
            .ExecuteUpdateAsync(s => s.SetProperty(c => c.RecordatorioEnviado, true));

        return filas == 1;
    }

    public Task<int> ContarActivasFuturasDePacienteAsync(int pacienteId, DateTime desde)
    {
        return _db.Citas.CountAsync(c =>
            c.PacienteId == pacienteId && c.FechaInicio >= desde &&
            (c.Estado == EstadosCita.Programada || c.Estado == EstadosCita.Confirmada));
    }

    public async Task<Dictionary<int, int>> ContarProximasPorPsicologoAsync(int[] psicologoIds, DateTime desde)
    {
        return await _db.Citas
            .AsNoTracking()
            .Where(c => psicologoIds.Contains(c.PsicologoId) && c.FechaInicio >= desde)
            .Where(c => c.Estado == EstadosCita.Programada || c.Estado == EstadosCita.Confirmada)
            .GroupBy(c => c.PsicologoId)
            .Select(g => new { Id = g.Key, Total = g.Count() })
            .ToDictionaryAsync(x => x.Id, x => x.Total);
    }
}
