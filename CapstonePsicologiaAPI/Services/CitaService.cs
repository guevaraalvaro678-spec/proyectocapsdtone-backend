using CapstonePsicologiaAPI.Data;
using CapstonePsicologiaAPI.DTOs;
using CapstonePsicologiaAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace CapstonePsicologiaAPI.Services;

public class CitaService : ICitaService
{
    private static readonly TimeSpan AnticipacionRecordatorio = TimeSpan.FromHours(24);

    private readonly ICitaData _citaData;
    private readonly IUsuarioData _usuarioData;
    private readonly ICorreoService _correo;

    public CitaService(ICitaData citaData, IUsuarioData usuarioData, ICorreoService correo)
    {
        _citaData = citaData;
        _usuarioData = usuarioData;
        _correo = correo;
    }

    public async Task<List<CitaDto>> ListarAsync(CitaFiltro filtro)
    {
        var citas = await _citaData.ListarAsync(filtro);
        return citas.Select(c => Mapeos.ACita(c, incluirNotas: true)).ToList();
    }

    public async Task<List<CitaDto>> MiasAsync(int usuarioId, string rol)
    {
        var esPsicologo = rol == Roles.Psicologo;
        var filtro = esPsicologo ? new CitaFiltro(PsicologoId: usuarioId) : new CitaFiltro(PacienteId: usuarioId);

        var citas = await _citaData.ListarAsync(filtro);
        return citas.Select(c => Mapeos.ACita(c, incluirNotas: esPsicologo)).ToList();
    }

    public async Task<CitaDto> CrearAsync(CitaCrearDto datos)
    {
        var paciente = await ObtenerPacienteAsync(datos.PacienteId);
        var psicologo = await ObtenerPsicologoAsync(datos.PsicologoId);
        var inicio = AUtc(datos.FechaInicio);

        if (inicio <= DateTime.UtcNow)
            throw ReglaNegocioException.Invalida("La cita debe ser en una fecha y hora futuras");

        await ValidarDisponibilidadAsync(psicologo.Id, paciente.Id, inicio, datos.DuracionMinutos, excluirId: null);

        var cita = new Cita
        {
            PacienteId = paciente.Id,
            PsicologoId = psicologo.Id,
            FechaInicio = inicio,
            DuracionMinutos = datos.DuracionMinutos,
            Modalidad = datos.Modalidad,
            Lugar = LimpiarOpcional(datos.Lugar),
            Motivo = datos.Motivo.Trim(),
            Estado = EstadosCita.Programada,
            FechaCreacion = DateTime.UtcNow,
        };

        try
        {
            await _citaData.CrearAsync(cita);
        }
        catch (DbUpdateException)
        {
            // El índice único de la base frena dos reservas simultáneas del mismo horario.
            throw ReglaNegocioException.Conflicto("Ese horario ya fue reservado por otra persona");
        }

        cita.Paciente = paciente;
        cita.Psicologo = psicologo;
        await NotificarAgendadaAsync(cita, reprogramada: false);

        return Mapeos.ACita(cita, incluirNotas: true);
    }

    public async Task<CitaDto> ActualizarAsync(int id, CitaCrearDto datos)
    {
        var cita = await _citaData.ObtenerPorIdAsync(id)
                   ?? throw ReglaNegocioException.NoEncontrada("Cita no encontrada");

        if (cita.Estado is EstadosCita.Realizada or EstadosCita.Cancelada)
            throw ReglaNegocioException.Invalida("No se puede modificar una cita cerrada");

        var paciente = await ObtenerPacienteAsync(datos.PacienteId);
        var psicologo = await ObtenerPsicologoAsync(datos.PsicologoId);
        var inicio = AUtc(datos.FechaInicio);

        var cambioHorario = inicio != AUtc(cita.FechaInicio)
                            || datos.DuracionMinutos != cita.DuracionMinutos
                            || psicologo.Id != cita.PsicologoId;

        if (cambioHorario)
        {
            if (inicio <= DateTime.UtcNow)
                throw ReglaNegocioException.Invalida("La cita debe ser en una fecha y hora futuras");

            await ValidarDisponibilidadAsync(psicologo.Id, paciente.Id, inicio, datos.DuracionMinutos, excluirId: cita.Id);
        }

        cita.Paciente = paciente;
        cita.Psicologo = psicologo;
        cita.PacienteId = paciente.Id;
        cita.PsicologoId = psicologo.Id;
        cita.FechaInicio = inicio;
        cita.DuracionMinutos = datos.DuracionMinutos;
        cita.Modalidad = datos.Modalidad;
        cita.Lugar = LimpiarOpcional(datos.Lugar);
        cita.Motivo = datos.Motivo.Trim();

        if (cambioHorario)
        {
            // Al reprogramar, el recordatorio se envía de nuevo y la cita vuelve a estar por confirmar.
            cita.RecordatorioEnviado = false;
            cita.Estado = EstadosCita.Programada;
        }

        await _citaData.ActualizarAsync(cita);

        if (cambioHorario)
            await NotificarAgendadaAsync(cita, reprogramada: true);

        return Mapeos.ACita(cita, incluirNotas: true);
    }

    public async Task<CitaDto> CambiarEstadoAsync(int id, CitaEstadoDto datos, int actorId, string actorRol)
    {
        var cita = await _citaData.ObtenerPorIdAsync(id)
                   ?? throw ReglaNegocioException.NoEncontrada("Cita no encontrada");

        var ahora = DateTime.UtcNow;
        var esAdmin = actorRol is Roles.SuperAdmin or Roles.Admin;
        var estadoAnterior = cita.Estado;

        if (!esAdmin)
        {
            if (actorRol == Roles.Psicologo)
            {
                if (cita.PsicologoId != actorId)
                    throw ReglaNegocioException.Prohibida("Esta cita no es tuya");

                if (datos.Estado == EstadosCita.Programada)
                    throw ReglaNegocioException.Invalida("Una cita no puede volver a estar solo programada");
            }
            else if (actorRol == Roles.Usuario)
            {
                if (cita.PacienteId != actorId)
                    throw ReglaNegocioException.Prohibida("Esta cita no es tuya");

                if (datos.Estado != EstadosCita.Cancelada)
                    throw ReglaNegocioException.Prohibida("Solo puedes cancelar tu cita");

                if (AUtc(cita.FechaInicio) <= ahora)
                    throw ReglaNegocioException.Invalida("La cita ya empezó, no se puede cancelar");
            }
            else
            {
                throw ReglaNegocioException.Prohibida("No tienes permiso para modificar citas");
            }

            if (estadoAnterior == EstadosCita.Cancelada)
                throw ReglaNegocioException.Invalida("La cita ya está cancelada");

            // Una cita realizada solo admite que su psicólogo siga completando las notas.
            if (estadoAnterior == EstadosCita.Realizada && !(actorRol == Roles.Psicologo && datos.Estado == EstadosCita.Realizada))
                throw ReglaNegocioException.Invalida("La cita ya fue realizada");
        }

        if (datos.Estado == EstadosCita.Realizada && AUtc(cita.FechaInicio) > ahora)
            throw ReglaNegocioException.Invalida("La cita todavía no ocurrió");

        cita.Estado = datos.Estado;

        // Las notas son del psicólogo: el paciente no las escribe ni las ve.
        if (actorRol != Roles.Usuario && datos.Notas is not null)
            cita.Notas = LimpiarOpcional(datos.Notas);

        await _citaData.ActualizarAsync(cita);

        if (datos.Estado == EstadosCita.Cancelada && estadoAnterior != EstadosCita.Cancelada)
            await NotificarCanceladaAsync(cita);

        if (datos.Estado == EstadosCita.Confirmada && estadoAnterior == EstadosCita.Programada)
            await NotificarConfirmadaAsync(cita);

        return Mapeos.ACita(cita, incluirNotas: actorRol != Roles.Usuario);
    }

    public async Task ProcesarRecordatoriosAsync()
    {
        // Sin SMTP no se marca nada como enviado: cuando se configure, los recordatorios pendientes saldrán.
        if (!_correo.SmtpConfigurado)
            return;

        var ahora = DateTime.UtcNow;
        var citas = await _citaData.ObtenerParaRecordatorioAsync(ahora, ahora + AnticipacionRecordatorio);

        foreach (var cita in citas)
        {
            if (!await _citaData.ReclamarRecordatorioAsync(cita.Id))
                continue;

            var paciente = cita.Paciente;

            if (paciente is { Correo: not null, Activo: true, RecordatoriosCorreo: true })
            {
                await _correo.EncolarAsync(
                    paciente.Correo,
                    paciente.Nombre,
                    "Recordatorio de tu cita",
                    PlantillasCorreo.RecordatorioCita(paciente.Nombre, cita.Psicologo?.Nombre ?? "tu psicólogo", cita.FechaInicio, cita.Modalidad, cita.Lugar, cita.Motivo, EnlacePaciente()),
                    TiposCorreo.RecordatorioCita);
            }
        }
    }

    // ---- Internos ----

    private async Task<Usuario> ObtenerPacienteAsync(int id)
    {
        var paciente = await _usuarioData.ObtenerPorIdAsync(id);

        if (paciente is null || paciente.Rol != Roles.Usuario || !paciente.Activo)
            throw ReglaNegocioException.Invalida("El paciente elegido no es válido");

        return paciente;
    }

    private async Task<Usuario> ObtenerPsicologoAsync(int id)
    {
        var psicologo = await _usuarioData.ObtenerPorIdAsync(id);

        if (psicologo is null || psicologo.Rol != Roles.Psicologo || !psicologo.Activo)
            throw ReglaNegocioException.Invalida("El psicólogo elegido no es válido");

        return psicologo;
    }

    private async Task ValidarDisponibilidadAsync(int psicologoId, int pacienteId, DateTime inicio, int minutos, int? excluirId)
    {
        var fin = inicio.AddMinutes(minutos);

        if (await _citaData.HaySolapamientoAsync(psicologoId, null, inicio, fin, excluirId))
            throw ReglaNegocioException.Conflicto("El psicólogo ya tiene una cita en ese horario");

        if (await _citaData.HaySolapamientoAsync(null, pacienteId, inicio, fin, excluirId))
            throw ReglaNegocioException.Conflicto("El paciente ya tiene una cita en ese horario");
    }

    private async Task NotificarAgendadaAsync(Cita cita, bool reprogramada)
    {
        // El paciente no recibe correo al solicitar: se le avisa cuando el psicólogo confirma la cita.
        var asunto = reprogramada ? "Una cita fue reprogramada" : "Nueva solicitud de cita";
        var paciente = cita.Paciente!;
        var psicologo = cita.Psicologo!;

        if (psicologo.Correo is not null)
        {
            await _correo.EncolarAsync(
                psicologo.Correo, psicologo.Nombre, asunto,
                PlantillasCorreo.NuevaCita(psicologo.Nombre, paciente.Nombre, true, cita.FechaInicio, cita.Modalidad, cita.Lugar, cita.Motivo, EnlacePsicologo()),
                TiposCorreo.NuevaCita);
        }
    }

    private async Task NotificarConfirmadaAsync(Cita cita)
    {
        var paciente = cita.Paciente!;
        var psicologo = cita.Psicologo!;

        if (paciente is { Correo: not null, RecordatoriosCorreo: true })
        {
            await _correo.EncolarAsync(
                paciente.Correo, paciente.Nombre, "Tu cita fue confirmada",
                PlantillasCorreo.CitaConfirmada(paciente.Nombre, psicologo.Nombre, cita.FechaInicio, cita.Modalidad, cita.Lugar, cita.Motivo, EnlacePaciente()),
                TiposCorreo.NuevaCita);
        }
    }

    private async Task NotificarCanceladaAsync(Cita cita)
    {
        var paciente = cita.Paciente!;
        var psicologo = cita.Psicologo!;

        if (paciente is { Correo: not null, RecordatoriosCorreo: true })
        {
            await _correo.EncolarAsync(
                paciente.Correo, paciente.Nombre, "Tu cita fue cancelada",
                PlantillasCorreo.CitaCancelada(paciente.Nombre, psicologo.Nombre, cita.FechaInicio, EnlacePaciente()),
                TiposCorreo.CitaCancelada);
        }

        if (psicologo.Correo is not null)
        {
            await _correo.EncolarAsync(
                psicologo.Correo, psicologo.Nombre, "Una cita fue cancelada",
                PlantillasCorreo.CitaCancelada(psicologo.Nombre, paciente.Nombre, cita.FechaInicio, EnlacePsicologo()),
                TiposCorreo.CitaCancelada);
        }
    }

    private string EnlacePaciente() => $"{_correo.FrontUrl}/mi-espacio?seccion=citas";
    private string EnlacePsicologo() => $"{_correo.FrontUrl}/psicologo/citas";

    /// <summary>El front envía UTC; si llegara sin zona se toma como UTC, sin conversiones.</summary>
    private static DateTime AUtc(DateTime fecha) =>
        fecha.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(fecha, DateTimeKind.Utc) : fecha.ToUniversalTime();

    private static string? LimpiarOpcional(string? valor) =>
        string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
}
