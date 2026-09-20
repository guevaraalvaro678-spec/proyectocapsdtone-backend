using CapstonePsicologiaAPI.Data;
using CapstonePsicologiaAPI.DTOs;
using CapstonePsicologiaAPI.Models;

namespace CapstonePsicologiaAPI.Services;

public class ReservaService : IReservaService
{
    /// <summary>Tope de citas pendientes por paciente, para que nadie acapare los horarios.</summary>
    private const int MaximoCitasPendientes = 3;

    private readonly IUsuarioData _usuarioData;
    private readonly IDisponibilidadData _disponibilidadData;
    private readonly IDisponibilidadService _disponibilidad;
    private readonly ICitaData _citaData;
    private readonly ICitaService _citas;

    public ReservaService(
        IUsuarioData usuarioData,
        IDisponibilidadData disponibilidadData,
        IDisponibilidadService disponibilidad,
        ICitaData citaData,
        ICitaService citas)
    {
        _usuarioData = usuarioData;
        _disponibilidadData = disponibilidadData;
        _disponibilidad = disponibilidad;
        _citaData = citaData;
        _citas = citas;
    }

    public async Task<List<PsicologoPublicoDto>> ListarPsicologosAsync()
    {
        var conHorario = await _disponibilidadData.ObtenerPsicologosConHorarioAsync();
        var psicologos = await _usuarioData.ListarAsync(new[] { Roles.Psicologo }, buscar: null, activo: true);

        return psicologos
            .Where(p => conHorario.Contains(p.Id))
            .OrderBy(p => p.Nombre)
            .Select(p => new PsicologoPublicoDto { Id = p.Id, Nombre = p.Nombre, Especialidad = p.Especialidad })
            .ToList();
    }

    public async Task<List<HorarioDisponibleDto>> ListarHorariosAsync(int psicologoId, int dias)
    {
        await ValidarPsicologoAsync(psicologoId);
        return await _disponibilidad.ObtenerHorariosAsync(psicologoId, dias);
    }

    public async Task<CitaDto> ReservarAsync(int pacienteId, ReservaCrearDto datos)
    {
        await ValidarPsicologoAsync(datos.PsicologoId);

        var inicio = datos.FechaInicio.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(datos.FechaInicio, DateTimeKind.Utc)
            : datos.FechaInicio.ToUniversalTime();

        // Solo se puede reservar un horario que el psicólogo realmente dejó libre y que sigue libre.
        var horario = await _disponibilidad.BuscarHorarioAsync(datos.PsicologoId, inicio)
                      ?? throw ReglaNegocioException.Conflicto("Ese horario ya no está disponible. Elige otro.");

        if (horario.Modalidad != ModalidadesEvento.Mixta && horario.Modalidad != datos.Modalidad)
            throw ReglaNegocioException.Invalida($"En ese horario solo se atiende de forma {horario.Modalidad.ToLowerInvariant()}");

        var pendientes = await _citaData.ContarActivasFuturasDePacienteAsync(pacienteId, DateTime.UtcNow);

        if (pendientes >= MaximoCitasPendientes)
            throw ReglaNegocioException.Conflicto($"Ya tienes {MaximoCitasPendientes} citas pendientes. Espera a atender alguna o cancela una para reservar otra.");

        // Se reutiliza el alta de citas: valida cruces y avisa por correo al paciente y al psicólogo.
        return await _citas.CrearAsync(new CitaCrearDto
        {
            PacienteId = pacienteId,
            PsicologoId = datos.PsicologoId,
            FechaInicio = inicio,
            DuracionMinutos = horario.DuracionMinutos,
            Modalidad = datos.Modalidad,
            Lugar = horario.Lugar,
            Motivo = datos.Motivo,
        });
    }

    private async Task ValidarPsicologoAsync(int psicologoId)
    {
        var psicologo = await _usuarioData.ObtenerPorIdAsync(psicologoId);

        if (psicologo is null || psicologo.Rol != Roles.Psicologo || !psicologo.Activo)
            throw ReglaNegocioException.Invalida("El psicólogo elegido no es válido");
    }
}
