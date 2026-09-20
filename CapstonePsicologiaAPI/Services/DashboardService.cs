using CapstonePsicologiaAPI.Data;
using CapstonePsicologiaAPI.DTOs;

namespace CapstonePsicologiaAPI.Services;

public class DashboardService : IDashboardService
{
    /// <summary>Lima está en UTC-5 todo el año (no usa horario de verano).</summary>
    private const int DesfaseLima = -5;

    private readonly IDashboardData _dashboardData;
    private readonly ICitaData _citaData;
    private readonly ICorreoData _correoData;

    public DashboardService(IDashboardData dashboardData, ICitaData citaData, ICorreoData correoData)
    {
        _dashboardData = dashboardData;
        _citaData = citaData;
        _correoData = correoData;
    }

    public async Task<DashboardDto> ObtenerAsync()
    {
        var ahora = DateTime.UtcNow;

        // "Hoy" y cada día se cuentan en hora de Lima: el día empieza a las 05:00 UTC.
        var hoyLima = ahora.AddHours(DesfaseLima).Date;
        var inicioHoy = DateTime.SpecifyKind(hoyLima.AddHours(-DesfaseLima), DateTimeKind.Utc);
        var finHoy = inicioHoy.AddDays(1);
        var fin7Dias = inicioHoy.AddDays(7);

        var fechasCitas = await _dashboardData.ObtenerFechasDeCitasAsync(inicioHoy, fin7Dias);
        var citasPorDia = Enumerable.Range(0, 7).Select(i =>
        {
            var dia = hoyLima.AddDays(i);
            return new CitasPorDiaDto
            {
                Fecha = dia.ToString("yyyy-MM-dd"),
                Cantidad = fechasCitas.Count(f => f.AddHours(DesfaseLima).Date == dia),
            };
        }).ToList();

        var proximas = await _citaData.ListarAsync(new CitaFiltro(Desde: ahora, Maximo: 5));

        return new DashboardDto
        {
            UsuariosActivos = await _dashboardData.ContarUsuariosActivosAsync(),
            PsicologosActivos = await _dashboardData.ContarPsicologosActivosAsync(),
            CitasHoy = await _dashboardData.ContarCitasAsync(inicioHoy, finHoy),
            CitasProximos7Dias = fechasCitas.Count,
            SolicitudesPendientes = await _dashboardData.ContarSolicitudesPendientesAsync(),
            MensajesSinLeer = await _dashboardData.ContarMensajesSinLeerAsync(),
            EventosPublicados = await _dashboardData.ContarEventosPublicadosAsync(),
            CorreosPendientes = await _correoData.ContarPendientesAsync(),
            CitasPorDia = citasPorDia,
            ProximasCitas = proximas.Select(c => Mapeos.ACita(c, incluirNotas: false)).ToList(),
        };
    }
}
