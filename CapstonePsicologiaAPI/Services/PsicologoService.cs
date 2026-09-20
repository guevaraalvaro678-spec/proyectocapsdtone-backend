using CapstonePsicologiaAPI.Data;
using CapstonePsicologiaAPI.DTOs;
using CapstonePsicologiaAPI.Models;
using Microsoft.AspNetCore.Identity;

namespace CapstonePsicologiaAPI.Services;

public class PsicologoService : IPsicologoService
{
    private const int MaximoCitasEnDetalle = 50;

    private readonly IUsuarioData _usuarioData;
    private readonly IColaboradorData _colaboradorData;
    private readonly ICitaData _citaData;
    private readonly ISolicitudData _solicitudData;
    private readonly IDisponibilidadData _disponibilidadData;
    private readonly IPasswordHasher<Usuario> _hasher;
    private readonly ICorreoService _correo;

    public PsicologoService(
        IUsuarioData usuarioData,
        IColaboradorData colaboradorData,
        ICitaData citaData,
        ISolicitudData solicitudData,
        IDisponibilidadData disponibilidadData,
        IPasswordHasher<Usuario> hasher,
        ICorreoService correo)
    {
        _usuarioData = usuarioData;
        _colaboradorData = colaboradorData;
        _citaData = citaData;
        _solicitudData = solicitudData;
        _disponibilidadData = disponibilidadData;
        _hasher = hasher;
        _correo = correo;
    }

    public async Task<List<PsicologoListaDto>> ListarAsync(string? buscar, bool? activo)
    {
        var psicologos = await _usuarioData.ListarAsync(new[] { Roles.Psicologo }, buscar, activo);
        var ids = psicologos.Select(p => p.Id).ToArray();

        var citas = await _citaData.ContarProximasPorPsicologoAsync(ids, DateTime.UtcNow);
        var solicitudes = await _solicitudData.ContarAsignadasAbiertasAsync(ids);

        return psicologos.Select(p =>
        {
            var dto = ALista(p);
            dto.CitasProximas = citas.GetValueOrDefault(p.Id);
            dto.SolicitudesAsignadas = solicitudes.GetValueOrDefault(p.Id);
            return dto;
        }).ToList();
    }

    public async Task<PsicologoDetalleDto> ObtenerDetalleAsync(int id)
    {
        var psicologo = await ObtenerPsicologoAsync(id);

        var citas = await _citaData.ListarAsync(new CitaFiltro(PsicologoId: id, Desde: DateTime.UtcNow.AddDays(-1), Maximo: MaximoCitasEnDetalle));
        var solicitudes = await _solicitudData.ObtenerAsignadasAbiertasAsync(id);
        var disponibilidad = await _disponibilidadData.ListarDePsicologoAsync(id, soloActivos: false);

        var detalle = new PsicologoDetalleDto
        {
            Disponibilidad = disponibilidad.Select(Mapeos.ADisponibilidad).ToList(),
            FechaRegistro = Mapeos.Utc(psicologo.FechaRegistro),
            Citas = citas.Select(c => Mapeos.ACita(c, incluirNotas: true)).ToList(),
            Solicitudes = solicitudes.Select(Mapeos.ASolicitud).ToList(),
            CitasProximas = citas.Count(c => c.Estado is EstadosCita.Programada or EstadosCita.Confirmada && Mapeos.Utc(c.FechaInicio) >= DateTime.UtcNow),
            SolicitudesAsignadas = solicitudes.Count,
        };

        Copiar(psicologo, detalle);
        return detalle;
    }

    public async Task<PsicologoListaDto> CrearAsync(PsicologoCrearDto datos)
    {
        var correo = datos.Correo.Trim().ToLowerInvariant();

        if (await _usuarioData.ExisteCorreoAsync(correo))
            throw ReglaNegocioException.Conflicto("Ese correo ya está registrado");

        var psicologo = new Usuario
        {
            NombreUsuario = correo,
            Correo = correo,
            Nombre = datos.Nombre.Trim(),
            Telefono = LimpiarOpcional(datos.Telefono),
            Especialidad = datos.Especialidad.Trim(),
            NumeroColegiatura = LimpiarOpcional(datos.NumeroColegiatura),
            Rol = Roles.Psicologo,
            FechaRegistro = DateTime.UtcNow,
        };
        psicologo.PasswordHash = _hasher.HashPassword(psicologo, datos.Password);

        await _usuarioData.CrearAsync(psicologo);
        await SincronizarPerfilAsync(psicologo, revisadoPorCreacion: true);

        await _correo.EncolarAsync(
            correo,
            psicologo.Nombre,
            "Se creó tu cuenta de psicólogo",
            PlantillasCorreo.BienvenidaPsicologo(psicologo.Nombre, $"{_correo.FrontUrl}/psicologo"),
            TiposCorreo.Bienvenida);

        return ALista(psicologo);
    }

    public async Task<PsicologoListaDto> ActualizarAsync(int id, PsicologoActualizarDto datos)
    {
        var psicologo = await ObtenerPsicologoAsync(id);

        psicologo.Nombre = datos.Nombre.Trim();
        psicologo.Telefono = LimpiarOpcional(datos.Telefono);
        psicologo.Especialidad = datos.Especialidad.Trim();
        psicologo.NumeroColegiatura = LimpiarOpcional(datos.NumeroColegiatura);

        await _usuarioData.ActualizarAsync(psicologo);
        await SincronizarPerfilAsync(psicologo, revisadoPorCreacion: false);
        return ALista(psicologo);
    }

    /// <summary>Mantiene la fila de la tabla Psicologos (perfil profesional) al día con la cuenta.</summary>
    private async Task SincronizarPerfilAsync(Usuario cuenta, bool revisadoPorCreacion)
    {
        var perfil = await _colaboradorData.ObtenerPorUsuarioIdAsync(cuenta.Id);

        // Si el correo ya tenía una postulación (p. ej. rechazada), se reutiliza esa fila.
        perfil ??= await _colaboradorData.ObtenerPorCorreoAsync(cuenta.Correo!) ?? new Psicologo { Correo = cuenta.Correo! };

        perfil.Nombre = cuenta.Nombre;
        perfil.Telefono = cuenta.Telefono;
        perfil.Especialidad = cuenta.Especialidad ?? string.Empty;
        perfil.NumeroColegiatura = cuenta.NumeroColegiatura;
        perfil.UsuarioId = cuenta.Id;
        perfil.PasswordHash = null;

        if (revisadoPorCreacion)
        {
            perfil.Estado = EstadosColaborador.Aprobado;
            perfil.MotivoRechazo = null;
            perfil.FechaRevision = DateTime.UtcNow;
        }

        if (perfil.Id == 0)
            await _colaboradorData.CrearAsync(perfil);
        else
            await _colaboradorData.GuardarAsync();
    }

    private async Task<Usuario> ObtenerPsicologoAsync(int id)
    {
        var usuario = await _usuarioData.ObtenerPorIdAsync(id);

        if (usuario is null || usuario.Rol != Roles.Psicologo)
            throw ReglaNegocioException.NoEncontrada("Psicólogo no encontrado");

        return usuario;
    }

    private static PsicologoListaDto ALista(Usuario u)
    {
        var dto = new PsicologoListaDto();
        Copiar(u, dto);
        return dto;
    }

    private static void Copiar(Usuario u, PsicologoListaDto destino)
    {
        destino.Id = u.Id;
        destino.Nombre = u.Nombre;
        destino.Correo = u.Correo;
        destino.Telefono = u.Telefono;
        destino.Especialidad = u.Especialidad;
        destino.NumeroColegiatura = u.NumeroColegiatura;
        destino.Activo = u.Activo;
        destino.UltimoAcceso = u.UltimoAcceso is null ? null : Mapeos.Utc(u.UltimoAcceso.Value);
    }

    private static string? LimpiarOpcional(string? valor) =>
        string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
}
