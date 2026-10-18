using CapstonePsicologiaAPI.Data;
using CapstonePsicologiaAPI.DTOs;
using CapstonePsicologiaAPI.Models;

namespace CapstonePsicologiaAPI.Services;

public interface IPostulacionService
{
    Task<ArchivoSubidoDto> SubirCvAsync(IFormFile archivo, int usuarioId);
    Task<Archivo> ObtenerArchivoAsync(int archivoId, int actorId, string actorRol);
    Task<PostulacionDto> CrearAsync(int usuarioId, PostulacionCrearDto datos);
    Task<PostulacionDto?> ObtenerMiaAsync(int usuarioId);
    Task<List<PostulacionDto>> ListarAsync(string? tipo, string? estado, string? buscar);
    Task<PostulacionDto> ObtenerAsync(int id);
    Task<PostulacionDto> CambiarEstadoAsync(int id, PostulacionEstadoDto datos);
}

/// <summary>
/// Proceso de selección: la persona postula, el equipo la habilita, la agenda en una sesión de entrevista grupal,
/// la evalúa y la acepta (pasa a ser voluntaria o practicante) o no la admite.
/// </summary>
public class PostulacionService : IPostulacionService
{
    private const int MaximoBytesCv = 10 * 1024 * 1024;
    private static readonly string[] RolesGestion = { Roles.SuperAdmin, Roles.Admin, Roles.RRHH };

    private readonly IPostulacionData _postulacionData;
    private readonly ISesionEntrevistaData _sesionData;
    private readonly IArchivoData _archivoData;
    private readonly IUsuarioData _usuarioData;
    private readonly IVoluntarioData _voluntarioData;
    private readonly IPracticanteData _practicanteData;
    private readonly ICorreoService _correo;

    public PostulacionService(
        IPostulacionData postulacionData,
        ISesionEntrevistaData sesionData,
        IArchivoData archivoData,
        IUsuarioData usuarioData,
        IVoluntarioData voluntarioData,
        IPracticanteData practicanteData,
        ICorreoService correo)
    {
        _postulacionData = postulacionData;
        _sesionData = sesionData;
        _archivoData = archivoData;
        _usuarioData = usuarioData;
        _voluntarioData = voluntarioData;
        _practicanteData = practicanteData;
        _correo = correo;
    }

    public async Task<ArchivoSubidoDto> SubirCvAsync(IFormFile archivo, int usuarioId)
    {
        if (archivo.Length == 0)
            throw ReglaNegocioException.Invalida("El archivo está vacío");

        if (archivo.Length > MaximoBytesCv)
            throw ReglaNegocioException.Invalida("El CV pesa más de 10 MB");

        if (archivo.ContentType != "application/pdf" || !archivo.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            throw ReglaNegocioException.Invalida("El CV debe ser un archivo PDF");

        using var memoria = new MemoryStream();
        await archivo.CopyToAsync(memoria);

        var guardado = await _archivoData.CrearAsync(new Archivo
        {
            Nombre = Path.GetFileName(archivo.FileName),
            TipoContenido = "application/pdf",
            Datos = memoria.ToArray(),
            SubidoPorUsuarioId = usuarioId,
        });

        return new ArchivoSubidoDto { Id = guardado.Id, Nombre = guardado.Nombre };
    }

    public async Task<Archivo> ObtenerArchivoAsync(int archivoId, int actorId, string actorRol)
    {
        var archivo = await _archivoData.ObtenerAsync(archivoId)
                      ?? throw ReglaNegocioException.NoEncontrada("Archivo no encontrado");

        // El CV lo ven el equipo de gestión y la persona que lo subió.
        if (!RolesGestion.Contains(actorRol) && archivo.SubidoPorUsuarioId != actorId)
            throw ReglaNegocioException.Prohibida("No tienes acceso a este archivo");

        return archivo;
    }

    public async Task<PostulacionDto> CrearAsync(int usuarioId, PostulacionCrearDto datos)
    {
        var usuario = await _usuarioData.ObtenerPorIdAsync(usuarioId)
                      ?? throw ReglaNegocioException.NoEncontrada("Cuenta no encontrada");

        if (usuario.Rol != Roles.Postulante)
            throw ReglaNegocioException.Prohibida("Tu cuenta ya forma parte de AMAAC");

        var anterior = await _postulacionData.ObtenerUltimaDeUsuarioAsync(usuarioId);
        if (anterior is not null && anterior.Estado != EstadosPostulacion.NoAdmitida)
            throw ReglaNegocioException.Conflicto("Ya tienes una postulación en curso");

        var esPracticante = datos.Tipo == TiposPostulacion.Practicante;
        if (esPracticante && (Vacio(datos.Universidad) || Vacio(datos.Carrera) || Vacio(datos.Ciclo)))
            throw ReglaNegocioException.Invalida("Para postular como practicante completa universidad, carrera y ciclo");

        if (datos.FechaNacimiento.Date > DateTime.UtcNow.Date.AddYears(-15))
            throw ReglaNegocioException.Invalida("Revisa tu fecha de nacimiento (debes tener al menos 15 años)");

        if (await _postulacionData.DniEnUsoAsync(datos.Dni, usuarioId))
            throw ReglaNegocioException.Conflicto("Ese DNI ya está registrado en otra cuenta");

        var cv = await _archivoData.ObtenerNombreAsync(datos.CvArchivoId!.Value);
        if (cv is null || (await _archivoData.ObtenerAsync(datos.CvArchivoId.Value))!.SubidoPorUsuarioId != usuarioId)
            throw ReglaNegocioException.Invalida("Vuelve a adjuntar tu CV");

        var postulacion = new Postulacion
        {
            // Código provisional único; se reemplaza por POS-AAAA-NNN cuando la base asigna el Id.
            Codigo = "TMP-" + Guid.NewGuid().ToString("N")[..16],
            UsuarioId = usuarioId,
            Tipo = datos.Tipo,
            Dni = datos.Dni,
            Nombres = datos.Nombres.Trim(),
            Apellidos = datos.Apellidos.Trim(),
            FechaNacimiento = datos.FechaNacimiento.Date,
            Direccion = datos.Direccion.Trim(),
            Distrito = datos.Distrito.Trim(),
            Provincia = datos.Provincia.Trim(),
            Telefono = datos.Telefono.Trim(),
            Universidad = Limpiar(datos.Universidad),
            Carrera = Limpiar(datos.Carrera),
            Ciclo = Limpiar(datos.Ciclo),
            Departamento = datos.Departamento.Trim(),
            DepartamentoSecundario = esPracticante ? Limpiar(datos.DepartamentoSecundario) : null,
            CvArchivoId = datos.CvArchivoId,
            ParticipaIniciativa = datos.ParticipaIniciativa,
            IniciativaDescripcion = datos.ParticipaIniciativa ? Limpiar(datos.IniciativaDescripcion) : null,
        };

        await _postulacionData.CrearAsync(postulacion);
        postulacion.Codigo = $"POS-{postulacion.FechaEnvio.Year}-{postulacion.Id:000}";

        // El nombre de la cuenta pasa a ser el nombre real de la persona.
        usuario.Nombre = postulacion.Nombres;
        usuario.Telefono = postulacion.Telefono;
        await _postulacionData.GuardarAsync();

        if (usuario.Correo is not null)
        {
            await _correo.EncolarAsync(usuario.Correo, usuario.Nombre, "Recibimos tu postulación",
                PlantillasCorreo.PostulacionEnviada(usuario.Nombre, postulacion.Codigo, postulacion.Tipo, EnlaceSeguimiento()),
                TiposCorreo.Postulacion);
        }

        postulacion.Usuario = usuario;
        return await ADtoAsync(postulacion, incluirCv: true);
    }

    public async Task<PostulacionDto?> ObtenerMiaAsync(int usuarioId)
    {
        var postulacion = await _postulacionData.ObtenerUltimaDeUsuarioAsync(usuarioId);
        return postulacion is null ? null : await ADtoAsync(postulacion, incluirCv: true);
    }

    public async Task<List<PostulacionDto>> ListarAsync(string? tipo, string? estado, string? buscar)
    {
        var lista = await _postulacionData.ListarAsync(tipo, estado, buscar);
        var dtos = new List<PostulacionDto>(lista.Count);
        foreach (var p in lista)
            dtos.Add(await ADtoAsync(p, incluirCv: false));
        return dtos;
    }

    public async Task<PostulacionDto> ObtenerAsync(int id) =>
        await ADtoAsync(await ObtenerEntidadAsync(id), incluirCv: true);

    public async Task<PostulacionDto> CambiarEstadoAsync(int id, PostulacionEstadoDto datos)
    {
        var postulacion = await ObtenerEntidadAsync(id);

        if (EstadosPostulacion.EsFinal(postulacion.Estado))
            throw ReglaNegocioException.Conflicto("Esta postulación ya tiene un resultado final");

        var usuario = postulacion.Usuario!;
        postulacion.NotaEvaluacion = Limpiar(datos.NotaEvaluacion) ?? postulacion.NotaEvaluacion;

        switch (datos.Estado)
        {
            case EstadosPostulacion.Recibida or EstadosPostulacion.HabilitadaEntrevista:
                postulacion.SesionEntrevistaId = null;
                break;

            case EstadosPostulacion.EntrevistaAgendada:
                await AsignarSesionAsync(postulacion, datos.SesionEntrevistaId);
                break;

            case EstadosPostulacion.EntrevistaRealizada or EstadosPostulacion.EnCalificacion:
                if (postulacion.SesionEntrevistaId is null)
                    throw ReglaNegocioException.Invalida("Primero agenda la entrevista de esta postulación");
                break;

            case EstadosPostulacion.Aceptada:
                await IncorporarAsync(postulacion, usuario);
                break;
        }

        postulacion.Estado = datos.Estado;
        postulacion.FechaActualizacion = DateTime.UtcNow;
        await _postulacionData.GuardarAsync();

        await NotificarAsync(postulacion, usuario);
        return await ADtoAsync(postulacion, incluirCv: true);
    }

    private async Task AsignarSesionAsync(Postulacion postulacion, int? sesionId)
    {
        if (sesionId is null)
            throw ReglaNegocioException.Invalida("Elige la sesión de entrevista");

        var sesion = await _sesionData.ObtenerPorIdAsync(sesionId.Value)
                     ?? throw ReglaNegocioException.NoEncontrada("Sesión no encontrada");

        if (sesion.Estado != EstadosSesion.Programada || sesion.Fin <= DateTime.UtcNow)
            throw ReglaNegocioException.Invalida("Esa sesión ya no está disponible");

        if (postulacion.SesionEntrevistaId != sesion.Id)
        {
            var inscritos = (await _sesionData.ContarInscritosAsync(new[] { sesion.Id })).GetValueOrDefault(sesion.Id);
            if (inscritos >= sesion.Cupo)
                throw ReglaNegocioException.Conflicto("La sesión ya no tiene cupos disponibles");
        }

        postulacion.SesionEntrevistaId = sesion.Id;
        postulacion.SesionEntrevista = sesion;
    }

    /// <summary>Aceptada: la cuenta pasa a Voluntario o Practicante y se crea su perfil con los datos de la postulación.</summary>
    private async Task IncorporarAsync(Postulacion p, Usuario usuario)
    {
        if (await _voluntarioData.ExisteDniAsync(p.Dni))
            throw ReglaNegocioException.Conflicto("Ese DNI ya pertenece a un miembro");

        if (p.Tipo == TiposPostulacion.Practicante)
        {
            if (Vacio(p.Universidad) || Vacio(p.Carrera) || Vacio(p.Ciclo))
                throw ReglaNegocioException.Invalida("Faltan los datos académicos del practicante");

            await _practicanteData.CrearAsync(new Practicante
            {
                Dni = p.Dni,
                UsuarioId = usuario.Id,
                Apellidos = p.Apellidos,
                Universidad = p.Universidad!,
                Carrera = p.Carrera!,
                Ciclo = p.Ciclo!,
                DepartamentoPrincipal = p.Departamento,
                DepartamentoSecundario = p.DepartamentoSecundario ?? p.Departamento,
            });
            usuario.Rol = Roles.Practicante;
        }
        else
        {
            await _voluntarioData.CrearAsync(new Voluntario { Dni = p.Dni, UsuarioId = usuario.Id, Apellidos = p.Apellidos });
            usuario.Rol = Roles.Voluntario;
        }

        usuario.Nombre = p.Nombres;
        usuario.Telefono = p.Telefono;
    }

    private async Task NotificarAsync(Postulacion p, Usuario usuario)
    {
        if (usuario.Correo is null)
            return;

        var (asunto, cuerpo) = p.Estado switch
        {
            EstadosPostulacion.EntrevistaAgendada when p.SesionEntrevista is not null => ("Tu entrevista grupal está agendada",
                PlantillasCorreo.EntrevistaAgendada(usuario.Nombre, p.SesionEntrevista.Nombre, p.SesionEntrevista.Inicio,
                    p.SesionEntrevista.Modalidad, p.SesionEntrevista.LugarOEnlace, EnlaceSeguimiento())),
            EstadosPostulacion.Aceptada => ("¡Tu postulación fue aceptada!",
                PlantillasCorreo.PostulacionAceptada(usuario.Nombre, p.Tipo, $"{_correo.FrontUrl}/login")),
            EstadosPostulacion.NoAdmitida => ("Sobre tu postulación a AMAAC Perú",
                PlantillasCorreo.PostulacionNoAdmitida(usuario.Nombre, p.NotaEvaluacion)),
            _ => ((string?)null, (string?)null),
        };

        if (asunto is not null && cuerpo is not null)
            await _correo.EncolarAsync(usuario.Correo, usuario.Nombre, asunto, cuerpo, TiposCorreo.Postulacion);
    }

    private async Task<Postulacion> ObtenerEntidadAsync(int id) =>
        await _postulacionData.ObtenerPorIdAsync(id) ?? throw ReglaNegocioException.NoEncontrada("Postulación no encontrada");

    private async Task<PostulacionDto> ADtoAsync(Postulacion p, bool incluirCv)
    {
        SesionEntrevistaDto? sesion = null;
        if (p.SesionEntrevista is { } s)
        {
            var inscritos = (await _sesionData.ContarInscritosAsync(new[] { s.Id })).GetValueOrDefault(s.Id);
            sesion = SesionEntrevistaService.ADto(s, inscritos);
        }

        return new PostulacionDto
        {
            Id = p.Id,
            Codigo = p.Codigo,
            UsuarioId = p.UsuarioId,
            Correo = p.Usuario?.Correo ?? p.Usuario?.NombreUsuario ?? string.Empty,
            Tipo = p.Tipo,
            Dni = p.Dni,
            Nombres = p.Nombres,
            Apellidos = p.Apellidos,
            FechaNacimiento = p.FechaNacimiento,
            Direccion = p.Direccion,
            Distrito = p.Distrito,
            Provincia = p.Provincia,
            Telefono = p.Telefono,
            Universidad = p.Universidad,
            Carrera = p.Carrera,
            Ciclo = p.Ciclo,
            Departamento = p.Departamento,
            DepartamentoSecundario = p.DepartamentoSecundario,
            CvArchivoId = p.CvArchivoId,
            CvNombre = incluirCv && p.CvArchivoId is not null ? await _archivoData.ObtenerNombreAsync(p.CvArchivoId.Value) : null,
            ParticipaIniciativa = p.ParticipaIniciativa,
            IniciativaDescripcion = p.IniciativaDescripcion,
            Estado = p.Estado,
            NotaEvaluacion = p.NotaEvaluacion,
            FechaEnvio = Mapeos.Utc(p.FechaEnvio),
            FechaActualizacion = Mapeos.Utc(p.FechaActualizacion),
            Sesion = sesion,
        };
    }

    private string EnlaceSeguimiento() => $"{_correo.FrontUrl}/mi-postulacion";

    private static bool Vacio(string? valor) => string.IsNullOrWhiteSpace(valor);
    private static string? Limpiar(string? valor) => string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
}
