using CapstonePsicologiaAPI.Data;
using CapstonePsicologiaAPI.DTOs;
using CapstonePsicologiaAPI.Models;

namespace CapstonePsicologiaAPI.Services;

public class SolicitudService : ISolicitudService
{
    private readonly ISolicitudData _solicitudData;
    private readonly IUsuarioData _usuarioData;
    private readonly ICorreoService _correo;

    public SolicitudService(ISolicitudData solicitudData, IUsuarioData usuarioData, ICorreoService correo)
    {
        _solicitudData = solicitudData;
        _usuarioData = usuarioData;
        _correo = correo;
    }

    public async Task<SolicitudDto?> CrearAsync(int usuarioId, SolicitudCrearDto datos)
    {
        if (await _solicitudData.TieneAbiertaAsync(usuarioId))
            return null;

        var solicitud = new SolicitudOrientacion
        {
            UsuarioId = usuarioId,
            Motivo = datos.Motivo.Trim(),
            Estado = EstadosSolicitud.Pendiente,
            FechaCreacion = DateTime.UtcNow,
            FechaActualizacion = DateTime.UtcNow,
        };

        await _solicitudData.CrearAsync(solicitud);

        // Se vuelve a leer para traer los datos del usuario que necesita el DTO.
        var creada = await _solicitudData.ObtenerUltimaDeUsuarioAsync(usuarioId);
        return creada is null ? null : Mapeos.ASolicitud(creada);
    }

    public async Task<SolicitudDto?> ObtenerMiaAsync(int usuarioId)
    {
        var solicitud = await _solicitudData.ObtenerUltimaDeUsuarioAsync(usuarioId);
        return solicitud is null ? null : Mapeos.ASolicitud(solicitud);
    }

    public async Task<List<SolicitudDto>> ListarAsync()
    {
        var solicitudes = await _solicitudData.ObtenerTodasAsync();
        return solicitudes.Select(Mapeos.ASolicitud).ToList();
    }

    public async Task<List<SolicitudDto>> ListarAsignadasAsync(int psicologoId)
    {
        var solicitudes = await _solicitudData.ObtenerAsignadasAbiertasAsync(psicologoId);
        return solicitudes.Select(Mapeos.ASolicitud).ToList();
    }

    public async Task<SolicitudDto?> CambiarEstadoAsync(int id, SolicitudEstadoDto datos)
    {
        var actual = await _solicitudData.ObtenerPorIdAsync(id);

        if (actual is null)
            return null;

        var asignando = datos.Estado == EstadosSolicitud.Asignada;

        if (asignando)
        {
            if (datos.PsicologoId is null)
                throw ReglaNegocioException.Invalida("Elige el psicólogo que atenderá la solicitud");

            var psicologo = await _usuarioData.ObtenerPorIdAsync(datos.PsicologoId.Value);

            if (psicologo is null || psicologo.Rol != Roles.Psicologo || !psicologo.Activo)
                throw ReglaNegocioException.Invalida("El psicólogo elegido no es válido");
        }

        var huboCambio = actual.Estado != datos.Estado
                         || (asignando && actual.PsicologoAsignadoId != datos.PsicologoId);

        var solicitud = await _solicitudData.CambiarEstadoAsync(id, datos.Estado, asignando ? datos.PsicologoId : null);

        if (solicitud is null)
            return null;

        // Se avisa al usuario de cada avance, pero no si solo se repitió el mismo estado ni al volver a "Pendiente".
        if (huboCambio && datos.Estado != EstadosSolicitud.Pendiente)
            await AvisarAlUsuarioAsync(solicitud);

        return Mapeos.ASolicitud(solicitud);
    }

    private async Task AvisarAlUsuarioAsync(SolicitudOrientacion solicitud)
    {
        var usuario = solicitud.Usuario;

        if (usuario is not { Correo: not null, RecordatoriosCorreo: true })
            return;

        await _correo.EncolarAsync(
            usuario.Correo,
            usuario.Nombre,
            "Novedades de tu solicitud de orientación",
            PlantillasCorreo.EstadoSolicitud(usuario.Nombre, solicitud.Estado, solicitud.PsicologoAsignado?.Nombre, $"{_correo.FrontUrl}/mi-espacio?seccion=solicitud"),
            TiposCorreo.EstadoSolicitud);
    }
}
