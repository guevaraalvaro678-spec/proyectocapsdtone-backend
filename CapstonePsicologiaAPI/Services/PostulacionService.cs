using CapstonePsicologiaAPI.Data;
using CapstonePsicologiaAPI.DTOs;
using CapstonePsicologiaAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace CapstonePsicologiaAPI.Services;

public interface IPostulacionService
{
    Task<ArchivoSubidoDto> SubirCvAsync(IFormFile archivo, int usuarioId);
    Task<Archivo> ObtenerArchivoAsync(int archivoId, int actorId, string actorRol);
    Task<PostulacionDto> CrearAsync(int usuarioId, PostulacionCrearDto datos);

    /// <summary>Crea la cuenta (si hace falta), guarda el CV y registra la postulación en una sola transacción.</summary>
    Task<PostulacionEnviadaDto> EnviarCompletaAsync(int? usuarioId, PostulacionCompletaDto datos, IFormFile? cv);
    Task<PostulacionDto?> ObtenerMiaAsync(int usuarioId);
    Task<List<PostulacionDto>> ListarAsync(string? tipo, string? estado, string? buscar);
    Task<PostulacionDto> ObtenerAsync(int id);
    Task<PostulacionDto> CambiarEstadoAsync(int id, PostulacionEstadoDto datos, int actorId);
    Task<PostulacionResumenDto> ResumenAsync(string? tipo, string? estado, string? buscar);

    /// <summary>Las postulaciones filtradas en CSV (se abre en Excel).</summary>
    Task<byte[]> ExportarCsvAsync(string? tipo, string? estado, string? buscar);
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
    private readonly ICuentaService _cuenta;
    private readonly AppDbContext _db;

    public PostulacionService(
        IPostulacionData postulacionData,
        ISesionEntrevistaData sesionData,
        IArchivoData archivoData,
        IUsuarioData usuarioData,
        IVoluntarioData voluntarioData,
        IPracticanteData practicanteData,
        ICorreoService correo,
        ICuentaService cuenta,
        AppDbContext db)
    {
        _postulacionData = postulacionData;
        _sesionData = sesionData;
        _archivoData = archivoData;
        _usuarioData = usuarioData;
        _voluntarioData = voluntarioData;
        _practicanteData = practicanteData;
        _correo = correo;
        _cuenta = cuenta;
        _db = db;
    }

    public async Task<PostulacionEnviadaDto> EnviarCompletaAsync(int? usuarioId, PostulacionCompletaDto datos, IFormFile? cv)
    {
        if (usuarioId is null && !datos.AceptaTerminos)
            throw ReglaNegocioException.Invalida("Acepta los términos y el tratamiento de tus datos para continuar.");

        // La transacción va dentro de la estrategia de reintentos que usa la conexión (EnableRetryOnFailure).
        var estrategia = _db.Database.CreateExecutionStrategy();
        return await estrategia.ExecuteAsync(async () =>
        {
            await using var transaccion = await _db.Database.BeginTransactionAsync();

            var usuario = usuarioId is null
                ? await _cuenta.ObtenerOCrearPostulanteAsync(datos.Correo, datos.Password, datos.GoogleCredential)
                : await _usuarioData.ObtenerPorIdAsync(usuarioId.Value) ?? throw ReglaNegocioException.NoEncontrada("Cuenta no encontrada");

            var sesion = usuarioId is null ? await _cuenta.RenovarSesionAsync(usuario.Id) : null;

            // El correo ya era de un miembro: no se postula, entra a su portal.
            if (usuario.Rol is Roles.Voluntario or Roles.Practicante)
            {
                await transaccion.CommitAsync();
                return new PostulacionEnviadaDto { Sesion = sesion, EsMiembro = true };
            }

            // El CV solo se pide a practicantes.
            datos.CvArchivoId = null;
            if (datos.Tipo == TiposPostulacion.Practicante)
            {
                if (cv is null)
                    throw ReglaNegocioException.Invalida("Adjunta tu CV en PDF");
                datos.CvArchivoId = (await SubirCvAsync(cv, usuario.Id)).Id;
            }

            var postulacion = await CrearAsync(usuario.Id, datos);

            await transaccion.CommitAsync();
            return new PostulacionEnviadaDto { Sesion = sesion, Postulacion = postulacion };
        });
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
        if (esPracticante)
            await ValidarDatosPracticanteAsync(usuarioId, datos);

        // Al voluntario solo se le piden nombres, apellidos y teléfono (y el correo de su cuenta).
        var postulacion = new Postulacion
        {
            // Código provisional único; se reemplaza por POS-AAAA-NNN cuando la base asigna el Id.
            Codigo = "TMP-" + Guid.NewGuid().ToString("N")[..16],
            UsuarioId = usuarioId,
            Tipo = datos.Tipo,
            Nombres = datos.Nombres.Trim(),
            Apellidos = datos.Apellidos.Trim(),
            Telefono = datos.Telefono.Trim(),
            Dni = esPracticante ? datos.Dni : null,
            FechaNacimiento = esPracticante ? datos.FechaNacimiento!.Value.Date : null,
            Direccion = esPracticante ? Limpiar(datos.Direccion) : null,
            Distrito = esPracticante ? Limpiar(datos.Distrito) : null,
            Provincia = esPracticante ? Limpiar(datos.Provincia) : null,
            Universidad = esPracticante ? Limpiar(datos.Universidad) : null,
            Carrera = esPracticante ? Limpiar(datos.Carrera) : null,
            Ciclo = esPracticante ? Limpiar(datos.Ciclo) : null,
            Departamento = esPracticante ? Limpiar(datos.Departamento) : null,
            DepartamentoSecundario = esPracticante ? Limpiar(datos.DepartamentoSecundario) : null,
            CvArchivoId = esPracticante ? datos.CvArchivoId : null,
            ParticipaIniciativa = esPracticante && datos.ParticipaIniciativa,
            IniciativaDescripcion = esPracticante && datos.ParticipaIniciativa ? Limpiar(datos.IniciativaDescripcion) : null,
        };

        await _postulacionData.CrearAsync(postulacion);
        postulacion.Codigo = $"POS-{postulacion.FechaEnvio.Year}-{postulacion.Id:000}";

        // El nombre de la cuenta pasa a ser el nombre real de la persona.
        usuario.Nombre = postulacion.Nombres;
        usuario.Telefono = postulacion.Telefono;
        _postulacionData.AgregarHistorial(new HistorialPostulacion
        {
            PostulacionId = postulacion.Id,
            Estado = postulacion.Estado,
            UsuarioId = usuarioId,
        });
        await _postulacionData.GuardarAsync();

        // CV con nombre estándar para ubicarlo fácil: CV_POS-2026-001_APELLIDOS.pdf
        if (postulacion.CvArchivoId is { } cvId)
            await _archivoData.RenombrarAsync(cvId, NombreCv(postulacion));

        if (usuario.Correo is not null)
        {
            await _correo.EncolarAsync(usuario.Correo, usuario.Nombre, "Recibimos tu postulación",
                PlantillasCorreo.PostulacionEnviada(usuario.Nombre, postulacion.Codigo, postulacion.Tipo, EnlaceSeguimiento()),
                TiposCorreo.Postulacion);
        }

        postulacion.Usuario = usuario;
        return await ADtoAsync(postulacion, incluirCv: true);
    }

    /// <summary>El practicante debe completar DNI, nacimiento, dirección, formación, área y CV.</summary>
    private async Task ValidarDatosPracticanteAsync(int usuarioId, PostulacionCrearDto datos)
    {
        if (Vacio(datos.Dni))
            throw ReglaNegocioException.Invalida("Ingresa tu DNI");

        if (datos.FechaNacimiento is null)
            throw ReglaNegocioException.Invalida("Ingresa tu fecha de nacimiento");

        if (datos.FechaNacimiento.Value.Date > DateTime.UtcNow.Date.AddYears(-15))
            throw ReglaNegocioException.Invalida("Revisa tu fecha de nacimiento (debes tener al menos 15 años)");

        if (Vacio(datos.Direccion) || Vacio(datos.Distrito) || Vacio(datos.Provincia))
            throw ReglaNegocioException.Invalida("Completa tu dirección, distrito y provincia");

        if (Vacio(datos.Universidad) || Vacio(datos.Carrera) || Vacio(datos.Ciclo))
            throw ReglaNegocioException.Invalida("Para postular como practicante completa universidad, carrera y ciclo");

        if (Vacio(datos.Departamento) || !AreasOng.Existe(datos.Departamento!))
            throw ReglaNegocioException.Invalida("Elige un área de la lista");

        if (!string.IsNullOrWhiteSpace(datos.DepartamentoSecundario) && !AreasOng.Existe(datos.DepartamentoSecundario))
            throw ReglaNegocioException.Invalida("El área secundaria no es válida");

        if (await _postulacionData.DniEnUsoAsync(datos.Dni!, usuarioId))
            throw ReglaNegocioException.Conflicto("Ese DNI ya está registrado en otra cuenta");

        if (datos.CvArchivoId is null)
            throw ReglaNegocioException.Invalida("Adjunta tu CV en PDF");

        var cv = await _archivoData.ObtenerAsync(datos.CvArchivoId.Value);
        if (cv is null || cv.SubidoPorUsuarioId != usuarioId)
            throw ReglaNegocioException.Invalida("Vuelve a adjuntar tu CV");
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

    public async Task<PostulacionResumenDto> ResumenAsync(string? tipo, string? estado, string? buscar)
    {
        var lista = await _postulacionData.ListarAsync(tipo, estado, buscar);
        int Contar(params string[] estados) => lista.Count(p => estados.Contains(p.Estado));

        return new PostulacionResumenDto
        {
            Total = lista.Count,
            Recibidas = Contar(EstadosPostulacion.Recibida),
            Habilitadas = Contar(EstadosPostulacion.HabilitadaEntrevista),
            Agendadas = Contar(EstadosPostulacion.EntrevistaAgendada),
            EnProceso = Contar(EstadosPostulacion.EntrevistaRealizada, EstadosPostulacion.EnCalificacion),
            Aceptadas = Contar(EstadosPostulacion.Aceptada),
            NoAdmitidas = Contar(EstadosPostulacion.NoAdmitida),
            RequierenAtencion = Contar(EstadosPostulacion.Recibida, EstadosPostulacion.HabilitadaEntrevista),
        };
    }

    public async Task<byte[]> ExportarCsvAsync(string? tipo, string? estado, string? buscar)
    {
        var lista = await _postulacionData.ListarAsync(tipo, estado, buscar);
        var csv = new System.Text.StringBuilder();
        static string Celda(string? valor) => $"\"{(valor ?? string.Empty).Replace("\"", "\"\"")}\"";
        void Fila(params string?[] celdas) => csv.Append(string.Join(';', celdas.Select(Celda))).Append("\r\n");

        Fila("Código", "Tipo", "DNI", "Nombres", "Apellidos", "Correo", "Teléfono", "Distrito", "Área", "Universidad", "Carrera", "Estado", "Fecha de envío");
        foreach (var p in lista)
        {
            Fila(p.Codigo, p.Tipo, p.Dni, p.Nombres, p.Apellidos, p.Usuario?.Correo ?? p.Usuario?.NombreUsuario, p.Telefono, p.Distrito,
                p.Departamento, p.Universidad, p.Carrera, EstadosPostulacion.Etiqueta(p.Estado), HoraLima(p.FechaEnvio).ToString("yyyy-MM-dd"));
        }

        // Con BOM para que Excel reconozca las tildes.
        return System.Text.Encoding.UTF8.GetPreamble().Concat(System.Text.Encoding.UTF8.GetBytes(csv.ToString())).ToArray();
    }

    /// <summary>Lima está en UTC-5 todo el año (sin horario de verano).</summary>
    private static DateTime HoraLima(DateTime utc) => utc.AddHours(-5);

    public async Task<PostulacionDto> ObtenerAsync(int id) =>
        await ADtoAsync(await ObtenerEntidadAsync(id), incluirCv: true);

    public async Task<PostulacionDto> CambiarEstadoAsync(int id, PostulacionEstadoDto datos, int actorId)
    {
        var postulacion = await ObtenerEntidadAsync(id);

        if (EstadosPostulacion.EsFinal(postulacion.Estado))
            throw ReglaNegocioException.Conflicto("Esta postulación ya tiene un resultado final");

        if (!EstadosPostulacion.PuedePasar(postulacion.Estado, datos.Estado))
            throw ReglaNegocioException.Invalida(
                $"No se puede pasar de \"{EstadosPostulacion.Etiqueta(postulacion.Estado)}\" a \"{EstadosPostulacion.Etiqueta(datos.Estado)}\"");

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
        _postulacionData.AgregarHistorial(new HistorialPostulacion
        {
            PostulacionId = postulacion.Id,
            Estado = datos.Estado,
            Nota = Limpiar(datos.NotaEvaluacion),
            UsuarioId = actorId,
        });
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
        if (p.Dni is not null && await _voluntarioData.ExisteDniAsync(p.Dni))
            throw ReglaNegocioException.Conflicto("Ese DNI ya pertenece a un miembro");

        if (p.Tipo == TiposPostulacion.Practicante)
        {
            if (Vacio(p.Dni) || Vacio(p.Universidad) || Vacio(p.Carrera) || Vacio(p.Ciclo) || Vacio(p.Departamento))
                throw ReglaNegocioException.Invalida("Faltan datos del practicante (DNI, formación o área)");

            await _practicanteData.CrearAsync(new Practicante
            {
                Dni = p.Dni!,
                UsuarioId = usuario.Id,
                Apellidos = p.Apellidos,
                Universidad = p.Universidad!,
                Carrera = p.Carrera!,
                Ciclo = p.Ciclo!,
                DepartamentoPrincipal = p.Departamento!,
                DepartamentoSecundario = p.DepartamentoSecundario ?? p.Departamento!,
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
            Historial = incluirCv
                ? (await _postulacionData.ListarHistorialAsync(p.Id)).Select(h => new HistorialPostulacionDto
                {
                    Estado = h.Estado,
                    Nota = h.Nota,
                    Responsable = h.Usuario is null ? null : string.IsNullOrWhiteSpace(h.Usuario.Nombre) ? h.Usuario.NombreUsuario : h.Usuario.Nombre,
                    Fecha = Mapeos.Utc(h.Fecha),
                }).ToList()
                : null,
            Acciones = EstadosPostulacion.Acciones(p.Estado),
        };
    }

    private static string NombreCv(Postulacion p)
    {
        var apellidos = string.Join('_', p.Apellidos.ToUpperInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries));
        var limpio = new string(apellidos.Normalize(System.Text.NormalizationForm.FormD)
            .Where(c => char.IsAsciiLetterOrDigit(c) || c == '_').ToArray());
        return $"CV_{p.Codigo}_{limpio}.pdf";
    }

    private string EnlaceSeguimiento() => $"{_correo.FrontUrl}/mi-postulacion";

    private static bool Vacio(string? valor) => string.IsNullOrWhiteSpace(valor);
    private static string? Limpiar(string? valor) => string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
}
