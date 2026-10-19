using CapstonePsicologiaAPI.Data;
using CapstonePsicologiaAPI.DTOs;
using CapstonePsicologiaAPI.Models;

namespace CapstonePsicologiaAPI.Services;

public interface ISesionEntrevistaService
{
    Task<List<SesionEntrevistaDto>> ListarAsync(string? estado);
    Task<SesionEntrevistaDetalleDto> ObtenerDetalleAsync(int id);
    Task<SesionEntrevistaDto> CrearAsync(SesionEntrevistaGuardarDto datos);
    Task<SesionEntrevistaDto> ActualizarAsync(int id, SesionEntrevistaGuardarDto datos);
    Task<SesionEntrevistaDto> CambiarEstadoAsync(int id, string estado);
}

/// <summary>Sesiones de entrevista grupal: el equipo las programa y asigna postulantes hasta llenar el cupo.</summary>
public class SesionEntrevistaService : ISesionEntrevistaService
{
    private readonly ISesionEntrevistaData _sesionData;
    private readonly IPostulacionData _postulacionData;
    private readonly IPostulacionService _postulacionService;

    public SesionEntrevistaService(ISesionEntrevistaData sesionData, IPostulacionData postulacionData, IPostulacionService postulacionService)
    {
        _sesionData = sesionData;
        _postulacionData = postulacionData;
        _postulacionService = postulacionService;
    }

    public async Task<List<SesionEntrevistaDto>> ListarAsync(string? estado)
    {
        var sesiones = await _sesionData.ListarAsync(estado);
        var inscritos = await _sesionData.ContarInscritosAsync(sesiones.Select(s => s.Id).ToArray());
        return sesiones.Select(s => ADto(s, inscritos.GetValueOrDefault(s.Id))).ToList();
    }

    public async Task<SesionEntrevistaDetalleDto> ObtenerDetalleAsync(int id)
    {
        var sesion = await ObtenerAsync(id);
        var postulantes = await _postulacionData.ListarDeSesionAsync(id);

        var detalle = new SesionEntrevistaDetalleDto();
        Copiar(ADto(sesion, postulantes.Count), detalle);

        foreach (var p in postulantes)
            detalle.Postulantes.Add(await _postulacionService.ObtenerAsync(p.Id));

        return detalle;
    }

    public async Task<SesionEntrevistaDto> CrearAsync(SesionEntrevistaGuardarDto datos)
    {
        Validar(datos);

        var sesion = new SesionEntrevista { Codigo = "TMP-" + Guid.NewGuid().ToString("N")[..16] };
        Aplicar(sesion, datos);
        await _sesionData.CrearAsync(sesion);

        sesion.Codigo = $"SES-{sesion.Id:000}";
        await _sesionData.GuardarAsync();
        return ADto(sesion, 0);
    }

    public async Task<SesionEntrevistaDto> ActualizarAsync(int id, SesionEntrevistaGuardarDto datos)
    {
        Validar(datos);
        var sesion = await ObtenerAsync(id);

        if (sesion.Estado != EstadosSesion.Programada)
            throw ReglaNegocioException.Invalida("Solo se pueden editar sesiones programadas");

        var inscritos = (await _sesionData.ContarInscritosAsync(new[] { id })).GetValueOrDefault(id);
        if (datos.Cupo < inscritos)
            throw ReglaNegocioException.Invalida($"La sesión ya tiene {inscritos} postulantes; el cupo no puede ser menor");

        Aplicar(sesion, datos);
        await _sesionData.GuardarAsync();
        return ADto(sesion, inscritos);
    }

    /// <summary>
    /// Realizada: sus postulantes agendados pasan a "Entrevista realizada".
    /// Cancelada: vuelven a "Habilitada para entrevista" para agendarlos en otra sesión.
    /// </summary>
    public async Task<SesionEntrevistaDto> CambiarEstadoAsync(int id, string estado)
    {
        var sesion = await ObtenerAsync(id);

        // Una sesión solo se cierra una vez: de Programada a Realizada o Cancelada.
        if (sesion.Estado != EstadosSesion.Programada)
            throw ReglaNegocioException.Conflicto("Esta sesión ya fue cerrada");

        if (estado == EstadosSesion.Realizada && sesion.Inicio > DateTime.UtcNow)
            throw ReglaNegocioException.Invalida("La sesión todavía no empieza; no se puede marcar como realizada");

        var postulantes = await _postulacionData.ListarDeSesionAsync(id);

        foreach (var p in postulantes.Where(p => p.Estado == EstadosPostulacion.EntrevistaAgendada))
        {
            if (estado == EstadosSesion.Realizada)
            {
                p.Estado = EstadosPostulacion.EntrevistaRealizada;
            }
            else if (estado == EstadosSesion.Cancelada)
            {
                p.Estado = EstadosPostulacion.HabilitadaEntrevista;
                p.SesionEntrevistaId = null;
            }

            p.FechaActualizacion = DateTime.UtcNow;
            if (estado is EstadosSesion.Realizada or EstadosSesion.Cancelada)
                _postulacionData.AgregarHistorial(new HistorialPostulacion
                {
                    PostulacionId = p.Id,
                    Estado = p.Estado,
                    Nota = $"Sesión {sesion.Codigo} {(estado == EstadosSesion.Realizada ? "realizada" : "cancelada")}",
                });
        }

        sesion.Estado = estado;
        await _sesionData.GuardarAsync();

        var inscritos = (await _sesionData.ContarInscritosAsync(new[] { id })).GetValueOrDefault(id);
        return ADto(sesion, inscritos);
    }

    private async Task<SesionEntrevista> ObtenerAsync(int id) =>
        await _sesionData.ObtenerPorIdAsync(id) ?? throw ReglaNegocioException.NoEncontrada("Sesión no encontrada");

    private static void Validar(SesionEntrevistaGuardarDto datos)
    {
        if (AUtc(datos.Fin) <= AUtc(datos.Inicio))
            throw ReglaNegocioException.Invalida("La hora de fin debe ser posterior a la de inicio");
    }

    private static void Aplicar(SesionEntrevista s, SesionEntrevistaGuardarDto d)
    {
        s.Nombre = d.Nombre.Trim();
        s.Inicio = AUtc(d.Inicio);
        s.Fin = AUtc(d.Fin);
        s.Modalidad = d.Modalidad;
        s.LugarOEnlace = d.LugarOEnlace.Trim();
        s.Cupo = d.Cupo;
    }

    private static DateTime AUtc(DateTime fecha) =>
        fecha.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(fecha, DateTimeKind.Utc) : fecha.ToUniversalTime();

    private static void Copiar(SesionEntrevistaDto origen, SesionEntrevistaDto destino)
    {
        destino.Id = origen.Id;
        destino.Codigo = origen.Codigo;
        destino.Nombre = origen.Nombre;
        destino.Inicio = origen.Inicio;
        destino.Fin = origen.Fin;
        destino.Modalidad = origen.Modalidad;
        destino.LugarOEnlace = origen.LugarOEnlace;
        destino.Cupo = origen.Cupo;
        destino.Inscritos = origen.Inscritos;
        destino.Estado = origen.Estado;
    }

    public static SesionEntrevistaDto ADto(SesionEntrevista s, int inscritos) => new()
    {
        Id = s.Id,
        Codigo = s.Codigo,
        Nombre = s.Nombre,
        Inicio = Mapeos.Utc(s.Inicio),
        Fin = Mapeos.Utc(s.Fin),
        Modalidad = s.Modalidad,
        LugarOEnlace = s.LugarOEnlace,
        Cupo = s.Cupo,
        Inscritos = inscritos,
        Estado = s.Estado,
        Disponible = s.Estado == EstadosSesion.Programada && s.Fin > DateTime.UtcNow && inscritos < s.Cupo,
        PorcentajeOcupacion = s.Cupo == 0 ? 0 : (int)Math.Round(inscritos * 100.0 / s.Cupo, MidpointRounding.AwayFromZero),
    };
}
