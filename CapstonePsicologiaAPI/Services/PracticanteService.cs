using CapstonePsicologiaAPI.Data;
using CapstonePsicologiaAPI.DTOs;
using CapstonePsicologiaAPI.Models;
using Microsoft.AspNetCore.Identity;

namespace CapstonePsicologiaAPI.Services;

public interface IPracticanteService
{
    Task<AdminSesionDto> RegistrarAsync(PracticanteRegistroDto datos);
    Task<PracticanteDto> ObtenerYoAsync(int usuarioId);
    Task<PracticanteDto> EnviarHorarioAsync(int usuarioId, HorarioEnviarDto datos);
    Task<List<PracticanteDto>> ListarAsync(string? estadoHorario, string? buscar);
    Task<PracticanteDto> RevisarHorarioAsync(int usuarioId, HorarioRevisionDto datos, int revisorId);
    Task<PracticanteDto> ConfirmarHorarioAsync(int usuarioId);
}

/// <summary>
/// Practicantes: envían su horario de la universidad y Recursos Humanos lo revisa (Pendiente, EnRevision,
/// Aceptado). Solo al aceptarlo se asigna el horario de prácticas, que el practicante ve confirmado.
/// </summary>
public class PracticanteService : IPracticanteService
{
    private readonly IPracticanteData _practicanteData;
    private readonly IVoluntarioData _voluntarioData;
    private readonly IUsuarioData _usuarioData;
    private readonly IPasswordHasher<Usuario> _hasher;
    private readonly ITokenService _tokenService;
    private readonly ICorreoService _correo;
    private readonly IGoogleAuthService _google;

    public PracticanteService(
        IPracticanteData practicanteData,
        IVoluntarioData voluntarioData,
        IUsuarioData usuarioData,
        IPasswordHasher<Usuario> hasher,
        ITokenService tokenService,
        ICorreoService correo,
        IGoogleAuthService google)
    {
        _practicanteData = practicanteData;
        _voluntarioData = voluntarioData;
        _usuarioData = usuarioData;
        _hasher = hasher;
        _tokenService = tokenService;
        _correo = correo;
        _google = google;
    }

    public async Task<AdminSesionDto> RegistrarAsync(PracticanteRegistroDto datos)
    {
        var correo = datos.Correo.Trim().ToLowerInvariant();

        if (await _usuarioData.ExisteCorreoAsync(correo))
            throw ReglaNegocioException.Conflicto("Ese correo ya está registrado");

        if (await _voluntarioData.ExisteDniAsync(datos.Dni))
            throw ReglaNegocioException.Conflicto("Ese DNI ya está registrado");

        // Con Google el correo ya viene verificado y no necesita contraseña propia.
        var conGoogle = !string.IsNullOrWhiteSpace(datos.GoogleCredential);
        if (conGoogle)
        {
            if ((await _google.ValidarAsync(datos.GoogleCredential!)).Correo != correo)
                throw ReglaNegocioException.Invalida("El correo debe ser el de tu cuenta de Google");
        }
        else if (datos.Password.Length < 6)
        {
            throw ReglaNegocioException.Invalida("La contraseña debe tener al menos 6 caracteres");
        }

        var usuario = new Usuario
        {
            NombreUsuario = correo,
            Correo = correo,
            Nombre = datos.Nombres.Trim(),
            Telefono = datos.Telefono.Trim(),
            Rol = Roles.Practicante,
            FechaRegistro = DateTime.UtcNow,
        };
        usuario.PasswordHash = _hasher.HashPassword(usuario, conGoogle ? Guid.NewGuid().ToString("N") : datos.Password);
        await _usuarioData.CrearAsync(usuario);

        await _practicanteData.CrearAsync(new Practicante
        {
            Dni = datos.Dni,
            UsuarioId = usuario.Id,
            Apellidos = datos.Apellidos.Trim(),
            Universidad = datos.Universidad.Trim(),
            Carrera = datos.Carrera.Trim(),
            Ciclo = datos.Ciclo.Trim(),
            DepartamentoPrincipal = datos.DepartamentoPrincipal.Trim(),
            DepartamentoSecundario = datos.DepartamentoSecundario.Trim(),
        });

        await _correo.EncolarAsync(correo, usuario.Nombre, "¡Bienvenido/a, practicante!",
            PlantillasCorreo.BienvenidaPracticante(usuario.Nombre, $"{_correo.FrontUrl}/practicante"),
            TiposCorreo.Bienvenida);

        return AdminService.ASesion(usuario, _tokenService);
    }

    public async Task<PracticanteDto> ObtenerYoAsync(int usuarioId)
    {
        var practicante = await ObtenerAsync(usuarioId);
        return (await ADtosAsync(new[] { practicante }))[0];
    }

    public async Task<PracticanteDto> EnviarHorarioAsync(int usuarioId, HorarioEnviarDto datos)
    {
        var practicante = await ObtenerAsync(usuarioId);
        ValidarBloques(datos.Bloques);

        await _practicanteData.ReemplazarBloquesAsync(usuarioId, TiposBloque.Universidad, ABloques(usuarioId, TiposBloque.Universidad, datos.Bloques));

        practicante.EstadoHorario = EstadosHorario.Pendiente;
        practicante.NotaHorario = null;
        practicante.FechaEnvioHorario = DateTime.UtcNow;
        practicante.FechaRevisionHorario = null;
        practicante.RevisadoPorUsuarioId = null;
        await _practicanteData.GuardarAsync();

        return (await ADtosAsync(new[] { practicante }))[0];
    }

    public async Task<List<PracticanteDto>> ListarAsync(string? estadoHorario, string? buscar) =>
        await ADtosAsync(await _practicanteData.ListarAsync(estadoHorario, buscar));

    public async Task<PracticanteDto> RevisarHorarioAsync(int usuarioId, HorarioRevisionDto datos, int revisorId)
    {
        var practicante = await ObtenerAsync(usuarioId);

        if (practicante.EstadoHorario == EstadosHorario.SinEnviar)
            throw ReglaNegocioException.Invalida("El practicante todavía no envió su horario");

        if (datos.Estado == EstadosHorario.Aceptado)
        {
            if (datos.HorarioPracticas is not { Count: > 0 })
                throw ReglaNegocioException.Invalida("Para aceptar, asigna el horario de prácticas");

            ValidarBloques(datos.HorarioPracticas);
            ValidarReglasPracticas(datos.HorarioPracticas);
            practicante.HorarioConfirmado = false;
            await _practicanteData.ReemplazarBloquesAsync(usuarioId, TiposBloque.Practicas, ABloques(usuarioId, TiposBloque.Practicas, datos.HorarioPracticas));
        }

        var yaAceptado = practicante.EstadoHorario == EstadosHorario.Aceptado;
        practicante.EstadoHorario = datos.Estado;
        practicante.NotaHorario = string.IsNullOrWhiteSpace(datos.Nota) ? null : datos.Nota.Trim();
        practicante.FechaRevisionHorario = DateTime.UtcNow;
        practicante.RevisadoPorUsuarioId = revisorId;
        await _practicanteData.GuardarAsync();

        if (datos.Estado == EstadosHorario.Aceptado && !yaAceptado && practicante.Usuario?.Correo is not null)
        {
            await _correo.EncolarAsync(practicante.Usuario.Correo, practicante.Usuario.Nombre, "Tu horario de prácticas fue confirmado",
                PlantillasCorreo.HorarioAceptado(practicante.Usuario.Nombre, $"{_correo.FrontUrl}/practicante/horario"),
                TiposCorreo.Bienvenida, revisorId);
        }

        return (await ADtosAsync(new[] { practicante }))[0];
    }

    /// <summary>El practicante confirma el horario de prácticas que le asignaron.</summary>
    public async Task<PracticanteDto> ConfirmarHorarioAsync(int usuarioId)
    {
        var practicante = await ObtenerAsync(usuarioId);

        if (practicante.EstadoHorario != EstadosHorario.Aceptado)
            throw ReglaNegocioException.Invalida("Todavía no tienes un horario de prácticas asignado");

        practicante.HorarioConfirmado = true;
        await _practicanteData.GuardarAsync();
        return (await ADtosAsync(new[] { practicante }))[0];
    }

    /// <summary>Reglas del horario de prácticas: lunes a sábado, máximo 5 h por día y 30 h por semana.</summary>
    private static void ValidarReglasPracticas(List<BloqueHorarioDto> bloques)
    {
        if (bloques.Any(b => b.DiaSemana == 7))
            throw ReglaNegocioException.Invalida("Las prácticas son de lunes a sábado");

        static double Horas(BloqueHorarioDto b) => (TimeSpan.Parse(b.HoraFin) - TimeSpan.Parse(b.HoraInicio)).TotalHours;

        var porDia = bloques.GroupBy(b => b.DiaSemana).Select(g => new { Dia = g.Key, Horas = g.Sum(Horas) });
        if (porDia.FirstOrDefault(d => d.Horas > 5) is { } excedido)
            throw ReglaNegocioException.Invalida($"Máximo 5 horas por día (el día {excedido.Dia} tiene {excedido.Horas:0.#} h)");

        var total = bloques.Sum(Horas);
        if (total > 30)
            throw ReglaNegocioException.Invalida($"Máximo 30 horas por semana (asignaste {total:0.#} h)");
    }

    private async Task<Practicante> ObtenerAsync(int usuarioId) =>
        await _practicanteData.ObtenerPorUsuarioIdAsync(usuarioId)
        ?? throw ReglaNegocioException.NoEncontrada("Practicante no encontrado");

    private static void ValidarBloques(List<BloqueHorarioDto> bloques)
    {
        foreach (var b in bloques)
        {
            if (string.CompareOrdinal(b.HoraInicio, b.HoraFin) >= 0)
                throw ReglaNegocioException.Invalida("En cada bloque la hora de inicio debe ser anterior a la de fin");
        }
    }

    private static List<BloqueHorario> ABloques(int usuarioId, string tipo, IEnumerable<BloqueHorarioDto> dtos) =>
        dtos.Select(b => new BloqueHorario
        {
            UsuarioId = usuarioId,
            Tipo = tipo,
            DiaSemana = b.DiaSemana,
            HoraInicio = b.HoraInicio,
            HoraFin = b.HoraFin,
            Modalidad = tipo == TiposBloque.Practicas ? b.Modalidad : null,
            Lugar = tipo == TiposBloque.Practicas && !string.IsNullOrWhiteSpace(b.Lugar) ? b.Lugar.Trim() : null,
        }).ToList();

    private async Task<List<PracticanteDto>> ADtosAsync(IReadOnlyCollection<Practicante> practicantes)
    {
        var bloques = await _practicanteData.ObtenerBloquesDeAsync(practicantes.Select(p => p.UsuarioId).ToArray());

        static List<BloqueHorarioDto> Filtrar(IEnumerable<BloqueHorario> todos, int id, string tipo) =>
            todos.Where(b => b.UsuarioId == id && b.Tipo == tipo)
                .OrderBy(b => b.DiaSemana).ThenBy(b => b.HoraInicio)
                .Select(b => new BloqueHorarioDto { DiaSemana = b.DiaSemana, HoraInicio = b.HoraInicio, HoraFin = b.HoraFin, Modalidad = b.Modalidad, Lugar = b.Lugar })
                .ToList();

        return practicantes.Select(p => new PracticanteDto
        {
            UsuarioId = p.UsuarioId,
            Dni = p.Dni,
            Nombre = p.Usuario!.Nombre,
            Apellidos = p.Apellidos,
            Correo = p.Usuario.Correo,
            Telefono = p.Usuario.Telefono,
            Universidad = p.Universidad,
            Carrera = p.Carrera,
            Ciclo = p.Ciclo,
            DepartamentoPrincipal = p.DepartamentoPrincipal,
            DepartamentoSecundario = p.DepartamentoSecundario,
            EstadoHorario = p.EstadoHorario,
            NotaHorario = p.NotaHorario,
            HorarioConfirmado = p.HorarioConfirmado,
            FechaEnvioHorario = p.FechaEnvioHorario is null ? null : Mapeos.Utc(p.FechaEnvioHorario.Value),
            Activo = p.Usuario.Activo,
            FechaRegistro = Mapeos.Utc(p.FechaRegistro),
            HorarioUniversidad = Filtrar(bloques, p.UsuarioId, TiposBloque.Universidad),
            HorarioPracticas = Filtrar(bloques, p.UsuarioId, TiposBloque.Practicas),
        }).ToList();
    }
}
