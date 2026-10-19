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
        var donaciones = await _donacionData.ContarPorEstadoAsync();

        int Cuenta(string estado) => postulaciones.GetValueOrDefault(estado);
        var total = postulaciones.Values.Sum();
        var voluntarios = activos.GetValueOrDefault(Roles.Voluntario);
        var practicantes = activos.GetValueOrDefault(Roles.Practicante);
        var miembros = voluntarios + practicantes;

        return new DashboardOngDto
        {
            Postulaciones = EstadosPostulacion.Todos.ToDictionary(e => e, Cuenta),
            TotalPostulaciones = total,
            Voluntarios = voluntarios,
            Practicantes = practicantes,
            PersonalAdministrativo = activos.GetValueOrDefault(Roles.SuperAdmin) + activos.GetValueOrDefault(Roles.Admin) + activos.GetValueOrDefault(Roles.RRHH),
            Donaciones = donaciones,
            IniciativasPendientes = await _db.Iniciativas.CountAsync(i => i.Estado == EstadosIniciativa.Pendiente),
            HorariosPendientes = await _db.Practicantes.CountAsync(p => p.EstadoHorario == EstadosHorario.Pendiente),
            ProximasSesiones = sesiones.Select(s => SesionEntrevistaService.ADto(s, inscritos.GetValueOrDefault(s.Id))).ToList(),
            Embudo = ArmarEmbudo(Cuenta, total),
            PendientesAtencion = Cuenta(EstadosPostulacion.Recibida) + Cuenta(EstadosPostulacion.HabilitadaEntrevista),
            EnEvaluacion = Cuenta(EstadosPostulacion.EntrevistaRealizada) + Cuenta(EstadosPostulacion.EnCalificacion),
            Miembros = miembros,
            PorcentajeVoluntarios = Porcentaje(voluntarios, miembros),
            ResumenDonaciones = ResumirDonaciones(donaciones),
        };
    }

    /// <summary>
    /// Embudo del proceso: una postulación que ya avanzó también "pasó" por las etapas anteriores,
    /// así que cada etapa cuenta las que están en ella o más adelante. Las dos últimas son los resultados finales.
    /// </summary>
    private static List<EtapaEmbudoDto> ArmarEmbudo(Func<string, int> cuenta, int total)
    {
        var estados = EstadosPostulacion.Todos;
        var finales = cuenta(EstadosPostulacion.Aceptada) + cuenta(EstadosPostulacion.NoAdmitida);
        int Desde(int indice) => estados.Skip(indice).Take(5 - indice).Sum(cuenta) + finales;

        return estados.Select((estado, i) =>
        {
            var valor = i switch { 0 => total, < 5 => Desde(i), _ => cuenta(estado) };
            return new EtapaEmbudoDto
            {
                Etiqueta = $"{i + 1}. {(i == 0 ? "Postulaciones recibidas" : EstadosPostulacion.Etiqueta(estado))}",
                Valor = valor,
                Porcentaje = Porcentaje(valor, total),
                Alerta = estado == EstadosPostulacion.EnCalificacion,
            };
        }).ToList();
    }

    private static ResumenDonacionesDto ResumirDonaciones(Dictionary<string, int> porEstado)
    {
        var pendientes = porEstado.GetValueOrDefault(EstadosDonacion.PendienteRecepcion);
        var recibidas = porEstado.GetValueOrDefault(EstadosDonacion.Recibida);
        var desestimadas = porEstado.GetValueOrDefault(EstadosDonacion.Desestimada);
        var total = pendientes + recibidas + desestimadas;
        decimal Pct(int n) => total == 0 ? 0 : Math.Round(n * 100m / total, 1, MidpointRounding.AwayFromZero);

        return new ResumenDonacionesDto
        {
            Total = total,
            Pendientes = pendientes,
            Recibidas = recibidas,
            Desestimadas = desestimadas,
            PorcentajePendientes = Pct(pendientes),
            PorcentajeRecibidas = Pct(recibidas),
            PorcentajeDesestimadas = Pct(desestimadas),
        };
    }

    private static int Porcentaje(int parte, int total) =>
        total == 0 ? 0 : (int)Math.Round(parte * 100.0 / total, MidpointRounding.AwayFromZero);
}
