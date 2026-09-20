using CapstonePsicologiaAPI.Data;
using CapstonePsicologiaAPI.DTOs;
using CapstonePsicologiaAPI.Models;

namespace CapstonePsicologiaAPI.Services;

public class DisponibilidadService : IDisponibilidadService
{
    /// <summary>Lima está en UTC-5 todo el año (no usa horario de verano).</summary>
    private const int DesfaseLima = -5;

    /// <summary>Antelación mínima para reservar: no se ofrecen horarios que empiezan en menos de este tiempo.</summary>
    private static readonly TimeSpan AntelacionMinima = TimeSpan.FromHours(2);

    public const int MaximoDias = 30;

    private readonly IDisponibilidadData _disponibilidadData;
    private readonly ICitaData _citaData;

    public DisponibilidadService(IDisponibilidadData disponibilidadData, ICitaData citaData)
    {
        _disponibilidadData = disponibilidadData;
        _citaData = citaData;
    }

    public async Task<List<DisponibilidadDto>> ListarDePsicologoAsync(int psicologoId)
    {
        var bloques = await _disponibilidadData.ListarDePsicologoAsync(psicologoId, soloActivos: false);
        return bloques.Select(Mapeos.ADisponibilidad).ToList();
    }

    public async Task<DisponibilidadDto> CrearAsync(int psicologoId, DisponibilidadGuardarDto datos)
    {
        var (inicio, fin) = ValidarHoras(datos);
        await ValidarQueNoSeCruceAsync(psicologoId, datos.DiaSemana, inicio, fin, excluirId: null, datos.Activo);

        var bloque = new Disponibilidad { PsicologoId = psicologoId };
        Aplicar(bloque, datos, inicio, fin);

        await _disponibilidadData.CrearAsync(bloque);
        return Mapeos.ADisponibilidad(bloque);
    }

    public async Task<DisponibilidadDto> ActualizarAsync(int id, int psicologoId, DisponibilidadGuardarDto datos)
    {
        var bloque = await ObtenerDeLaPersonaAsync(id, psicologoId);

        var (inicio, fin) = ValidarHoras(datos);
        await ValidarQueNoSeCruceAsync(psicologoId, datos.DiaSemana, inicio, fin, excluirId: id, datos.Activo);

        Aplicar(bloque, datos, inicio, fin);
        await _disponibilidadData.ActualizarAsync(bloque);
        return Mapeos.ADisponibilidad(bloque);
    }

    public async Task EliminarAsync(int id, int psicologoId)
    {
        var bloque = await ObtenerDeLaPersonaAsync(id, psicologoId);
        await _disponibilidadData.EliminarAsync(bloque);
    }

    public Task<List<HorarioDisponibleDto>> ObtenerHorariosAsync(int psicologoId, int dias)
    {
        var diasValidos = Math.Clamp(dias, 1, MaximoDias);
        return GenerarAsync(psicologoId, HoyLima(), diasValidos);
    }

    public async Task<HorarioDisponibleDto?> BuscarHorarioAsync(int psicologoId, DateTime inicioUtc)
    {
        var utc = inicioUtc.Kind == DateTimeKind.Utc ? inicioUtc : DateTime.SpecifyKind(inicioUtc, DateTimeKind.Utc);
        var diaLocal = utc.AddHours(DesfaseLima).Date;

        if (diaLocal < HoyLima() || (diaLocal - HoyLima()).TotalDays >= MaximoDias)
            return null;

        var horarios = await GenerarAsync(psicologoId, diaLocal, 1);
        return horarios.FirstOrDefault(h => h.Inicio == utc);
    }

    // ---- Generación de horarios ----

    /// <summary>
    /// Convierte los bloques semanales en horarios concretos: recorre cada día, parte cada bloque en citas
    /// consecutivas y descarta las que ya pasaron, empiezan muy pronto o se cruzan con una cita existente.
    /// </summary>
    private async Task<List<HorarioDisponibleDto>> GenerarAsync(int psicologoId, DateTime primerDiaLocal, int dias)
    {
        var bloques = await _disponibilidadData.ListarDePsicologoAsync(psicologoId, soloActivos: true);

        if (bloques.Count == 0)
            return new List<HorarioDisponibleDto>();

        var desdeUtc = DateTime.SpecifyKind(primerDiaLocal.AddHours(-DesfaseLima), DateTimeKind.Utc);
        var hastaUtc = desdeUtc.AddDays(dias);

        // Se traen también las citas que empezaron poco antes del rango y pueden seguir en curso.
        var ocupadas = (await _citaData.ListarAsync(new CitaFiltro(PsicologoId: psicologoId, Desde: desdeUtc.AddHours(-3), Hasta: hastaUtc)))
            .Where(c => c.Estado is EstadosCita.Programada or EstadosCita.Confirmada)
            .Select(c => (Inicio: Mapeos.Utc(c.FechaInicio), Fin: Mapeos.Utc(c.FechaInicio).AddMinutes(c.DuracionMinutos)))
            .ToList();

        var primeraHora = DateTime.UtcNow + AntelacionMinima;
        var horarios = new List<HorarioDisponibleDto>();

        for (var i = 0; i < dias; i++)
        {
            var dia = primerDiaLocal.AddDays(i);

            foreach (var bloque in bloques.Where(b => b.DiaSemana == (int)dia.DayOfWeek))
            {
                var duracion = TimeSpan.FromMinutes(bloque.DuracionMinutos);

                for (var hora = bloque.HoraInicio; hora + duracion <= bloque.HoraFin; hora += duracion)
                {
                    var inicio = DateTime.SpecifyKind(dia.Add(hora).AddHours(-DesfaseLima), DateTimeKind.Utc);
                    var fin = inicio + duracion;

                    if (inicio < primeraHora)
                        continue;

                    if (ocupadas.Any(o => o.Inicio < fin && o.Fin > inicio))
                        continue;

                    horarios.Add(new HorarioDisponibleDto
                    {
                        Inicio = inicio,
                        DuracionMinutos = bloque.DuracionMinutos,
                        Modalidad = bloque.Modalidad,
                        Lugar = bloque.Lugar,
                    });
                }
            }
        }

        return horarios.OrderBy(h => h.Inicio).ToList();
    }

    // ---- Validaciones ----

    private static (TimeSpan inicio, TimeSpan fin) ValidarHoras(DisponibilidadGuardarDto datos)
    {
        var inicio = TimeSpan.ParseExact(datos.HoraInicio, @"hh\:mm", null);
        var fin = TimeSpan.ParseExact(datos.HoraFin, @"hh\:mm", null);

        if (fin <= inicio)
            throw ReglaNegocioException.Invalida("La hora de fin debe ser posterior a la de inicio");

        if ((fin - inicio).TotalMinutes < datos.DuracionMinutos)
            throw ReglaNegocioException.Invalida("El bloque es más corto que la duración de una cita");

        return (inicio, fin);
    }

    private async Task ValidarQueNoSeCruceAsync(int psicologoId, int diaSemana, TimeSpan inicio, TimeSpan fin, int? excluirId, bool activo)
    {
        if (!activo)
            return;

        var existentes = await _disponibilidadData.ListarDePsicologoAsync(psicologoId, soloActivos: true);

        var seCruza = existentes.Any(b =>
            b.Id != excluirId && b.DiaSemana == diaSemana && inicio < b.HoraFin && fin > b.HoraInicio);

        if (seCruza)
            throw ReglaNegocioException.Conflicto("Ese horario se cruza con otro bloque que ya tienes ese día");
    }

    private async Task<Disponibilidad> ObtenerDeLaPersonaAsync(int id, int psicologoId)
    {
        var bloque = await _disponibilidadData.ObtenerPorIdAsync(id)
                     ?? throw ReglaNegocioException.NoEncontrada("Horario no encontrado");

        if (bloque.PsicologoId != psicologoId)
            throw ReglaNegocioException.Prohibida("Este horario no es tuyo");

        return bloque;
    }

    private static void Aplicar(Disponibilidad bloque, DisponibilidadGuardarDto datos, TimeSpan inicio, TimeSpan fin)
    {
        bloque.DiaSemana = datos.DiaSemana;
        bloque.HoraInicio = inicio;
        bloque.HoraFin = fin;
        bloque.DuracionMinutos = datos.DuracionMinutos;
        bloque.Modalidad = datos.Modalidad;
        bloque.Lugar = string.IsNullOrWhiteSpace(datos.Lugar) ? null : datos.Lugar.Trim();
        bloque.Activo = datos.Activo;
    }

    private static DateTime HoyLima() => DateTime.UtcNow.AddHours(DesfaseLima).Date;
}
