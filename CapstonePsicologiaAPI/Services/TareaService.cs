using CapstonePsicologiaAPI.Data;
using CapstonePsicologiaAPI.DTOs;
using CapstonePsicologiaAPI.Models;

namespace CapstonePsicologiaAPI.Services;

public interface ITareaService
{
    Task<TareaDto> CrearAsync(TareaCrearDto datos, int creadorId);
    Task<List<TareaDto>> ListarAsync(int? usuarioId);
    Task<TareaDto> CambiarEstadoAsync(int id, string estado, int actorId, string actorRol);
}

/// <summary>Actividades que el administrador o Recursos Humanos dejan a voluntarios y practicantes.</summary>
public class TareaService : ITareaService
{
    private readonly ITareaData _tareaData;
    private readonly IUsuarioData _usuarioData;

    public TareaService(ITareaData tareaData, IUsuarioData usuarioData)
    {
        _tareaData = tareaData;
        _usuarioData = usuarioData;
    }

    public async Task<TareaDto> CrearAsync(TareaCrearDto datos, int creadorId)
    {
        var destino = await _usuarioData.ObtenerPorIdAsync(datos.UsuarioId);

        if (destino is null || !destino.Activo || destino.Rol is not (Roles.Voluntario or Roles.Practicante))
            throw ReglaNegocioException.Invalida("La tarea solo se puede asignar a un voluntario o practicante activo");

        var tarea = new Tarea
        {
            UsuarioId = destino.Id,
            Titulo = datos.Titulo.Trim(),
            Descripcion = string.IsNullOrWhiteSpace(datos.Descripcion) ? null : datos.Descripcion.Trim(),
            FechaLimite = datos.FechaLimite?.ToUniversalTime(),
            Urgente = datos.Urgente,
            CreadaPorUsuarioId = creadorId,
        };

        await _tareaData.CrearAsync(tarea);
        tarea.Usuario = destino;
        return ADto(tarea);
    }

    public async Task<List<TareaDto>> ListarAsync(int? usuarioId) =>
        (await _tareaData.ListarAsync(usuarioId)).Select(ADto).ToList();

    public async Task<TareaDto> CambiarEstadoAsync(int id, string estado, int actorId, string actorRol)
    {
        var tarea = await _tareaData.ObtenerPorIdAsync(id)
                    ?? throw ReglaNegocioException.NoEncontrada("Tarea no encontrada");

        var esGestor = actorRol is Roles.SuperAdmin or Roles.Admin or Roles.RRHH;

        if (!esGestor && tarea.UsuarioId != actorId)
            throw ReglaNegocioException.Prohibida("Esta tarea no es tuya");

        tarea.Estado = estado;
        await _tareaData.GuardarAsync();
        return ADto(tarea);
    }

    private static TareaDto ADto(Tarea t) => new()
    {
        Id = t.Id,
        UsuarioId = t.UsuarioId,
        UsuarioNombre = t.Usuario?.Nombre ?? string.Empty,
        UsuarioRol = t.Usuario?.Rol ?? string.Empty,
        Titulo = t.Titulo,
        Descripcion = t.Descripcion,
        FechaLimite = t.FechaLimite is null ? null : Mapeos.Utc(t.FechaLimite.Value),
        Urgente = t.Urgente,
        Estado = t.Estado,
        FechaCreacion = Mapeos.Utc(t.FechaCreacion),
    };
}
