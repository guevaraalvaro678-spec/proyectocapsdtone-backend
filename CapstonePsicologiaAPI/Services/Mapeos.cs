using CapstonePsicologiaAPI.DTOs;
using CapstonePsicologiaAPI.Models;

namespace CapstonePsicologiaAPI.Services;

/// <summary>Conversión de entidades a DTOs que comparten varios servicios.</summary>
public static class Mapeos
{
    /// <summary>SQL Server devuelve las fechas sin zona; se marcan como UTC para que el front las convierta bien.</summary>
    public static DateTime Utc(DateTime fecha) => DateTime.SpecifyKind(fecha, DateTimeKind.Utc);

    public static CitaDto ACita(Cita c, bool incluirNotas) => new()
    {
        Id = c.Id,
        PacienteId = c.PacienteId,
        PacienteNombre = c.Paciente?.Nombre ?? string.Empty,
        PacienteCorreo = c.Paciente?.Correo,
        PsicologoId = c.PsicologoId,
        PsicologoNombre = c.Psicologo?.Nombre ?? string.Empty,
        FechaInicio = Utc(c.FechaInicio),
        DuracionMinutos = c.DuracionMinutos,
        Modalidad = c.Modalidad,
        Lugar = c.Lugar,
        Motivo = c.Motivo,
        Estado = c.Estado,
        Notas = incluirNotas ? c.Notas : null,
        FechaCreacion = Utc(c.FechaCreacion),
    };

    public static DisponibilidadDto ADisponibilidad(Disponibilidad d) => new()
    {
        Id = d.Id,
        PsicologoId = d.PsicologoId,
        DiaSemana = d.DiaSemana,
        HoraInicio = d.HoraInicio.ToString(@"hh\:mm"),
        HoraFin = d.HoraFin.ToString(@"hh\:mm"),
        DuracionMinutos = d.DuracionMinutos,
        Modalidad = d.Modalidad,
        Lugar = d.Lugar,
        Activo = d.Activo,
    };

    public static SolicitudDto ASolicitud(SolicitudOrientacion s) => new()
    {
        Id = s.Id,
        UsuarioId = s.UsuarioId,
        UsuarioNombre = s.Usuario?.Nombre ?? string.Empty,
        UsuarioCorreo = s.Usuario?.Correo,
        Motivo = s.Motivo,
        Estado = s.Estado,
        PsicologoAsignadoId = s.PsicologoAsignadoId,
        PsicologoAsignadoNombre = s.PsicologoAsignado?.Nombre,
        FechaCreacion = Utc(s.FechaCreacion),
        FechaActualizacion = Utc(s.FechaActualizacion),
    };
}
