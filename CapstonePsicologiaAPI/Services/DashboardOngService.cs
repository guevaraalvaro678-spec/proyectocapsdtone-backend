using CapstonePsicologiaAPI.Data;
using CapstonePsicologiaAPI.DTOs;
using CapstonePsicologiaAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace CapstonePsicologiaAPI.Services;

public interface IDashboardOngService
{
    Task<DashboardOngDto> ObtenerAsync();
    Task<BandejaRrhhDto> ObtenerBandejaAsync();

    /// <summary>Indicadores del proceso calculados en la base por el procedimiento sp_IndicadoresProceso.</summary>
    Task<IndicadoresProcesoDto> ObtenerIndicadoresAsync();
}

/// <summary>Indicadores del panel: proceso de selección, comunidad, donaciones y próximas entrevistas.</summary>
public class DashboardOngService : IDashboardOngService
{
    private const int MaximoSesiones = 5;
    private const int MaximoPendientes = 8;

    /// <summary>Plazo de respuesta a un postulante: pasado esto la postulación sale como atrasada.</summary>
    private const int DiasLimiteRespuesta = 3;

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

    public async Task<BandejaRrhhDto> ObtenerBandejaAsync()
    {
        var ahora = DateTime.UtcNow;
        var limite = ahora.AddDays(-DiasLimiteRespuesta);
        var abiertas = new[] { EstadosPostulacion.Recibida, EstadosPostulacion.HabilitadaEntrevista, EstadosPostulacion.EntrevistaRealizada, EstadosPostulacion.EnCalificacion };
        var conteo = await _postulacionData.ContarPorEstadoAsync();
        int Cuenta(params string[] estados) => estados.Sum(e => conteo.GetValueOrDefault(e));
        int Dias(DateTime desde) => Math.Max(0, (int)(ahora - desde).TotalDays);

        var postulaciones = await _db.Postulaciones.AsNoTracking()
            .Where(p => abiertas.Contains(p.Estado))
            .OrderBy(p => p.FechaActualizacion)
            .Take(MaximoPendientes)
            .Select(p => new { p.Id, p.Codigo, p.Nombres, p.Apellidos, p.Tipo, p.Estado, p.FechaActualizacion })
            .ToListAsync();

        var horarios = await _db.Practicantes.AsNoTracking()
            .Where(p => p.EstadoHorario == EstadosHorario.Pendiente || p.EstadoHorario == EstadosHorario.EnRevision)
            .OrderBy(p => p.FechaEnvioHorario)
            .Select(p => new { p.UsuarioId, p.Usuario!.Nombre, p.Apellidos, p.Universidad, p.EstadoHorario, p.FechaEnvioHorario, p.FechaRegistro })
            .ToListAsync();

        var tareas = await _db.Tareas.AsNoTracking()
            .Where(t => t.Estado == EstadosTarea.Pendiente && t.FechaLimite != null && t.FechaLimite < ahora)
            .OrderBy(t => t.FechaLimite)
            .Select(t => new { t.Id, t.Titulo, t.Urgente, t.FechaLimite, t.Usuario!.Nombre })
            .ToListAsync();

        // Desde dos semanas atrás: así aparecen primero las sesiones que ya pasaron y nadie cerró.
        var sesiones = await _sesionData.ProximasAsync(ahora.AddDays(-14), MaximoSesiones);
        var inscritos = await _sesionData.ContarInscritosAsync(sesiones.Select(s => s.Id).ToArray());

        return new BandejaRrhhDto
        {
            PorRevisar = Cuenta(EstadosPostulacion.Recibida),
            PorAgendar = Cuenta(EstadosPostulacion.HabilitadaEntrevista),
            PorDecidir = Cuenta(EstadosPostulacion.EntrevistaRealizada, EstadosPostulacion.EnCalificacion),
            Atrasadas = await _db.Postulaciones.CountAsync(p => abiertas.Contains(p.Estado) && p.FechaActualizacion < limite),
            DiasLimiteRespuesta = DiasLimiteRespuesta,
            SesionesPorCerrar = await _db.SesionesEntrevista.CountAsync(s => s.Estado == EstadosSesion.Programada && s.Fin < ahora),
            HorariosPorRevisar = horarios.Count,
            TareasVencidas = tareas.Count,
            EnTurnoAhora = await _db.Marcaciones.CountAsync(m => m.Salida == null && m.Entrada > ahora.AddHours(-16)),
            Postulaciones = postulaciones.Select(p => new PendienteBandejaDto
            {
                Id = p.Id,
                Titulo = $"{p.Nombres} {p.Apellidos}",
                Detalle = $"{p.Codigo} · {(p.Tipo == TiposPostulacion.Practicante ? "Practicante" : "Voluntario")} · {SiguientePaso(p.Estado, p.Tipo)}",
                Fecha = Mapeos.Utc(p.FechaActualizacion),
                DiasEspera = Dias(p.FechaActualizacion),
                Atrasado = p.FechaActualizacion < limite,
            }).ToList(),
            Sesiones = sesiones.Select(s => SesionEntrevistaService.ADto(s, inscritos.GetValueOrDefault(s.Id))).ToList(),
            Horarios = horarios.Take(MaximoPendientes).Select(h =>
            {
                var desde = h.FechaEnvioHorario ?? h.FechaRegistro;
                return new PendienteBandejaDto
                {
                    Id = h.UsuarioId,
                    Titulo = $"{h.Nombre} {h.Apellidos}",
                    Detalle = $"{h.Universidad} · {(h.EstadoHorario == EstadosHorario.EnRevision ? "En revisión" : "Horario enviado, falta asignar prácticas")}",
                    Fecha = Mapeos.Utc(desde),
                    DiasEspera = Dias(desde),
                    Atrasado = desde < limite,
                };
            }).ToList(),
            Tareas = tareas.Take(MaximoPendientes).Select(t => new PendienteBandejaDto
            {
                Id = t.Id,
                Titulo = t.Titulo,
                Detalle = $"Asignada a {t.Nombre}{(t.Urgente ? " · Urgente" : string.Empty)}",
                Fecha = Mapeos.Utc(t.FechaLimite!.Value),
                DiasEspera = Dias(t.FechaLimite.Value),
                Atrasado = true,
            }).ToList(),
        };
    }

    public async Task<IndicadoresProcesoDto> ObtenerIndicadoresAsync() =>
        (await _db.Database.SqlQuery<IndicadoresProcesoDto>($"EXEC dbo.sp_IndicadoresProceso").ToListAsync()).FirstOrDefault()
        ?? new IndicadoresProcesoDto();

    private static string SiguientePaso(string estado, string tipo) => estado switch
    {
        EstadosPostulacion.Recibida when tipo == TiposPostulacion.Voluntario => "Aceptar o citar a entrevista",
        EstadosPostulacion.Recibida => "Revisar y agendar entrevista",
        EstadosPostulacion.HabilitadaEntrevista => "Asignar sesión de entrevista",
        _ => "Decidir: aceptar o no admitir",
    };

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
        // Las entregadas también se recibieron.
        var recibidas = porEstado.GetValueOrDefault(EstadosDonacion.Recibida) + porEstado.GetValueOrDefault(EstadosDonacion.Entregada);
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
