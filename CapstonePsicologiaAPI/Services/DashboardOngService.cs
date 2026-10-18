using CapstonePsicologiaAPI.Data;
using CapstonePsicologiaAPI.DTOs;
using CapstonePsicologiaAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace CapstonePsicologiaAPI.Services;

public interface IDashboardOngService
{
    Task<DashboardOngDto> ObtenerAsync();
}

/// <summary>Indicadores del panel: proceso de selección, comunidad, donaciones y próximas entrevistas.</summary>
public class DashboardOngService : IDashboardOngService
{
    private const int MaximoSesiones = 5;

    private readonly AppDbContext _db;
    private readonly IPostulacionData _postulacionData;
    private readonly IDonacionData _donacionData;
    private readonly ISesionEntrevistaData _sesionData;

    public DashboardOngService(AppDbContext db, IPostulacionData postulacionData, IDonacionData donacionData, ISesionEntrevistaData sesionData)
    {
        _db = db;
        _postulacionData = postulacionData;
        _donacionData = donacionData;
        _sesionData = sesionData;
    }

    public async Task<DashboardOngDto> ObtenerAsync()
    {
        var postulaciones = await _postulacionData.ContarPorEstadoAsync();
        var activos = await _db.Usuarios.AsNoTracking().Where(u => u.Activo)
            .GroupBy(u => u.Rol).Select(g => new { g.Key, Total = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Total);

        var sesiones = await _sesionData.ProximasAsync(DateTime.UtcNow, MaximoSesiones);
        var inscritos = await _sesionData.ContarInscritosAsync(sesiones.Select(s => s.Id).ToArray());

        return new DashboardOngDto
        {
            Postulaciones = EstadosPostulacion.Todos.ToDictionary(e => e, e => postulaciones.GetValueOrDefault(e)),
            TotalPostulaciones = postulaciones.Values.Sum(),
            Voluntarios = activos.GetValueOrDefault(Roles.Voluntario),
            Practicantes = activos.GetValueOrDefault(Roles.Practicante),
            PersonalAdministrativo = activos.GetValueOrDefault(Roles.SuperAdmin) + activos.GetValueOrDefault(Roles.Admin) + activos.GetValueOrDefault(Roles.RRHH),
            Donaciones = await _donacionData.ContarPorEstadoAsync(),
            IniciativasPendientes = await _db.Iniciativas.CountAsync(i => i.Estado == EstadosIniciativa.Pendiente),
            HorariosPendientes = await _db.Practicantes.CountAsync(p => p.EstadoHorario == EstadosHorario.Pendiente),
            ProximasSesiones = sesiones.Select(s => SesionEntrevistaService.ADto(s, inscritos.GetValueOrDefault(s.Id))).ToList(),
        };
    }
}
