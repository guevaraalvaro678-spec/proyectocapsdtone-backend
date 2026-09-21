using CapstonePsicologiaAPI.Data;
using CapstonePsicologiaAPI.DTOs;
using CapstonePsicologiaAPI.Models;
using Microsoft.AspNetCore.Identity;

namespace CapstonePsicologiaAPI.Services;

public interface IColaboradorService
{
    Task PostularAsync(ColaboradorPostulacionDto datos);
    Task<List<ColaboradorDto>> ListarAsync(string? estado);
    Task<ColaboradorDto> AprobarAsync(int id, int administradorId);
    Task<ColaboradorDto> RechazarAsync(int id, string? motivo, int administradorId);
}

/// <summary>
/// Solicitudes de psicólogos colaboradores: postulan desde el sitio público (sin cuenta) y el administrador
/// las aprueba o rechaza. Solo al aprobar se crea la cuenta de acceso con rol Psicologo.
/// </summary>
public class ColaboradorService : IColaboradorService
{
    private readonly IColaboradorData _colaboradorData;
    private readonly IUsuarioData _usuarioData;
    private readonly IVoluntarioData _voluntarioData;
    private readonly IPasswordHasher<Usuario> _hasher;
    private readonly ICorreoService _correo;

    public ColaboradorService(
        IColaboradorData colaboradorData,
        IUsuarioData usuarioData,
        IVoluntarioData voluntarioData,
        IPasswordHasher<Usuario> hasher,
        ICorreoService correo)
    {
        _colaboradorData = colaboradorData;
        _usuarioData = usuarioData;
        _voluntarioData = voluntarioData;
        _hasher = hasher;
        _correo = correo;
    }

    public async Task PostularAsync(ColaboradorPostulacionDto datos)
    {
        var correo = datos.Correo.Trim().ToLowerInvariant();

        if (await _usuarioData.ExisteCorreoAsync(correo))
            throw ReglaNegocioException.Conflicto("Ese correo ya tiene una cuenta. Usa otro correo o inicia sesión.");

        var existente = await _colaboradorData.ObtenerPorCorreoAsync(correo);

        // El DNI no puede estar en otro perfil (voluntario u otro psicólogo); sí puede ser el de su propia postulación rechazada.
        if (existente?.Dni != datos.Dni && await _voluntarioData.ExisteDniAsync(datos.Dni))
            throw ReglaNegocioException.Conflicto("Ese DNI ya está registrado");

        if (existente is not null && existente.Estado != EstadosColaborador.Rechazado)
            throw ReglaNegocioException.Conflicto("Ya existe una postulación con ese correo.");

        // Quien fue rechazado puede volver a postular: se reutiliza su fila.
        var psicologo = existente ?? new Psicologo { Correo = correo };
        psicologo.Dni = datos.Dni;
        psicologo.Nombre = datos.Nombre.Trim();
        psicologo.PasswordHash = _hasher.HashPassword(new Usuario(), datos.Password);
        psicologo.Telefono = Limpiar(datos.Telefono);
        psicologo.Especialidad = datos.Especialidad.Trim();
        psicologo.NumeroColegiatura = Limpiar(datos.NumeroColegiatura);
        psicologo.Presentacion = Limpiar(datos.Presentacion);
        psicologo.Estado = EstadosColaborador.Pendiente;
        psicologo.MotivoRechazo = null;
        psicologo.FechaSolicitud = DateTime.UtcNow;
        psicologo.FechaRevision = null;
        psicologo.RevisadoPorUsuarioId = null;

        if (existente is null)
            await _colaboradorData.CrearAsync(psicologo);
        else
            await _colaboradorData.GuardarAsync();

        await _correo.EncolarAsync(correo, psicologo.Nombre, "Recibimos tu postulación",
            PlantillasCorreo.PostulacionRecibida(psicologo.Nombre), TiposCorreo.Bienvenida);
    }

    public async Task<List<ColaboradorDto>> ListarAsync(string? estado) =>
        (await _colaboradorData.ListarAsync(estado)).Select(ADto).ToList();

    public async Task<ColaboradorDto> AprobarAsync(int id, int administradorId)
    {
        var psicologo = await ObtenerPendienteAsync(id);

        if (await _usuarioData.ExisteCorreoAsync(psicologo.Correo))
            throw ReglaNegocioException.Conflicto("Ese correo ya tiene una cuenta; no se puede crear otra.");

        var cuenta = new Usuario
        {
            NombreUsuario = psicologo.Correo,
            Correo = psicologo.Correo,
            Nombre = psicologo.Nombre,
            Telefono = psicologo.Telefono,
            Especialidad = psicologo.Especialidad,
            NumeroColegiatura = psicologo.NumeroColegiatura,
            PasswordHash = psicologo.PasswordHash ?? string.Empty,
            Rol = Roles.Psicologo,
            FechaRegistro = DateTime.UtcNow,
        };
        await _usuarioData.CrearAsync(cuenta);

        psicologo.UsuarioId = cuenta.Id;
        psicologo.PasswordHash = null;
        psicologo.Estado = EstadosColaborador.Aprobado;
        psicologo.FechaRevision = DateTime.UtcNow;
        psicologo.RevisadoPorUsuarioId = administradorId;
        await _colaboradorData.GuardarAsync();

        await _correo.EncolarAsync(psicologo.Correo, psicologo.Nombre, "Tu postulación fue aprobada",
            PlantillasCorreo.PostulacionAprobada(psicologo.Nombre, $"{_correo.FrontUrl}/psicologo"),
            TiposCorreo.Bienvenida, administradorId);

        return ADto(psicologo);
    }

    public async Task<ColaboradorDto> RechazarAsync(int id, string? motivo, int administradorId)
    {
        var psicologo = await ObtenerPendienteAsync(id);

        psicologo.Estado = EstadosColaborador.Rechazado;
        psicologo.MotivoRechazo = Limpiar(motivo);
        psicologo.PasswordHash = null;
        psicologo.FechaRevision = DateTime.UtcNow;
        psicologo.RevisadoPorUsuarioId = administradorId;
        await _colaboradorData.GuardarAsync();

        await _correo.EncolarAsync(psicologo.Correo, psicologo.Nombre, "Sobre tu postulación",
            PlantillasCorreo.PostulacionRechazada(psicologo.Nombre, psicologo.MotivoRechazo),
            TiposCorreo.Bienvenida, administradorId);

        return ADto(psicologo);
    }

    private async Task<Psicologo> ObtenerPendienteAsync(int id)
    {
        var psicologo = await _colaboradorData.ObtenerPorIdAsync(id)
            ?? throw ReglaNegocioException.NoEncontrada("Solicitud no encontrada");

        if (psicologo.Estado != EstadosColaborador.Pendiente)
            throw ReglaNegocioException.Conflicto("Esta solicitud ya fue revisada.");

        return psicologo;
    }

    private static ColaboradorDto ADto(Psicologo p) => new()
    {
        Id = p.Id,
        Dni = p.Dni,
        Nombre = p.Nombre,
        Correo = p.Correo,
        Telefono = p.Telefono,
        Especialidad = p.Especialidad,
        NumeroColegiatura = p.NumeroColegiatura,
        Presentacion = p.Presentacion,
        Estado = p.Estado,
        MotivoRechazo = p.MotivoRechazo,
        FechaSolicitud = Mapeos.Utc(p.FechaSolicitud),
        FechaRevision = p.FechaRevision is null ? null : Mapeos.Utc(p.FechaRevision.Value),
        UsuarioId = p.UsuarioId,
    };

    private static string? Limpiar(string? valor) => string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
}
