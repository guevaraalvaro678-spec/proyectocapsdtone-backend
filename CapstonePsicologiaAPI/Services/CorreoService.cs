using CapstonePsicologiaAPI.Data;
using CapstonePsicologiaAPI.DTOs;
using CapstonePsicologiaAPI.Models;
using Microsoft.Extensions.Options;

namespace CapstonePsicologiaAPI.Services;

public class CorreoService : ICorreoService
{
    private const int MaximoDestinatarios = 500;
    private const int CorreosPorTanda = 20;
    private const int MaximoHistorial = 200;

    private readonly ICorreoData _correoData;
    private readonly IUsuarioData _usuarioData;
    private readonly ICorreoEnvio _envio;
    private readonly CorreoOpciones _opciones;
    private readonly ILogger<CorreoService> _logger;

    public CorreoService(
        ICorreoData correoData,
        IUsuarioData usuarioData,
        ICorreoEnvio envio,
        IOptions<CorreoOpciones> opciones,
        ILogger<CorreoService> logger)
    {
        _correoData = correoData;
        _usuarioData = usuarioData;
        _envio = envio;
        _opciones = opciones.Value;
        _logger = logger;
    }

    public bool SmtpConfigurado => _envio.Configurado;

    public string FrontUrl => _opciones.FrontUrl.TrimEnd('/');

    public async Task EncolarAsync(string destinatario, string? nombre, string asunto, string cuerpoHtml, string tipo, int? creadoPor = null, bool propagarErrores = false)
    {
        try
        {
            await _correoData.CrearVariosAsync(new[]
            {
                Nuevo(destinatario, nombre, asunto, cuerpoHtml, tipo, creadoPor, lote: null),
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "No se pudo dejar en la bandeja de salida el correo '{Asunto}' para {Destinatario}", asunto, destinatario);

            if (propagarErrores)
                throw;
        }
    }

    public async Task<CorreoEnvioResultadoDto?> EnviarAsync(CorreoEnviarDto datos, int administradorId)
    {
        var lote = Guid.NewGuid().ToString();
        var correos = new List<CorreoSaliente>();

        if (datos.Audiencia == "Individual")
        {
            var correo = datos.Correo!.Trim().ToLowerInvariant();
            correos.Add(Nuevo(correo, null, datos.Asunto.Trim(), PlantillasCorreo.Masivo(correo, datos.Mensaje), TiposCorreo.Masivo, administradorId, lote));
        }
        else
        {
            var roles = datos.Audiencia switch
            {
                "Usuarios" => new[] { Roles.Voluntario, Roles.Practicante },
                "Psicologos" => new[] { Roles.RRHH },
                "Administradores" => new[] { Roles.SuperAdmin, Roles.Admin },
                _ => new[] { Roles.Voluntario, Roles.Practicante, Roles.RRHH, Roles.SuperAdmin, Roles.Admin },
            };

            var destinatarios = await _usuarioData.ObtenerDestinatariosAsync(roles, respetarPreferencia: !datos.IncluirSinPreferencia);

            foreach (var usuario in destinatarios.Take(MaximoDestinatarios))
            {
                correos.Add(Nuevo(
                    usuario.Correo!,
                    usuario.Nombre,
                    datos.Asunto.Trim(),
                    PlantillasCorreo.Masivo(usuario.Nombre, datos.Mensaje),
                    TiposCorreo.Masivo,
                    administradorId,
                    lote));
            }
        }

        if (correos.Count == 0)
            return null;

        await _correoData.CrearVariosAsync(correos);
        return new CorreoEnvioResultadoDto { Destinatarios = correos.Count, Lote = lote };
    }

    public async Task<List<CorreoDto>> HistorialAsync()
    {
        var correos = await _correoData.ObtenerHistorialAsync(MaximoHistorial);

        return correos.Select(c => new CorreoDto
        {
            Id = c.Id,
            Destinatario = c.Destinatario,
            NombreDestinatario = c.NombreDestinatario,
            Asunto = c.Asunto,
            Tipo = c.Tipo,
            Estado = c.Estado,
            Error = c.Error,
            Lote = c.Lote,
            FechaCreacion = Mapeos.Utc(c.FechaCreacion),
            FechaEnvio = c.FechaEnvio is null ? null : Mapeos.Utc(c.FechaEnvio.Value),
        }).ToList();
    }

    public async Task<CorreoEstadoDto> ObtenerEstadoAsync()
    {
        return new CorreoEstadoDto
        {
            SmtpConfigurado = SmtpConfigurado,
            Pendientes = await _correoData.ContarPendientesAsync(),
        };
    }

    public Task<bool> ReintentarAsync(int id) => _correoData.ReintentarAsync(id);

    public async Task ProcesarPendientesAsync(CancellationToken ct)
    {
        // Sin SMTP no se toca la cola: así un servidor sin configurar no le quita el trabajo a otro que sí lo está.
        if (!_envio.Configurado)
            return;

        var pendientes = await _correoData.ObtenerPendientesAsync(CorreosPorTanda);

        foreach (var correo in pendientes)
        {
            ct.ThrowIfCancellationRequested();

            if (!await _correoData.ReclamarAsync(correo.Id))
                continue;

            try
            {
                await _envio.EnviarAsync(correo.Destinatario, correo.NombreDestinatario, correo.Asunto, correo.Cuerpo, ct);
                await _correoData.MarcarEnviadoAsync(correo.Id);
            }
            catch (OperationCanceledException)
            {
                await _correoData.MarcarFallidoAsync(correo.Id, "Se detuvo el servidor durante el envío");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "No se pudo enviar el correo {Id} a {Destinatario}", correo.Id, correo.Destinatario);
                await _correoData.MarcarFallidoAsync(correo.Id, ex.Message);
            }
        }
    }

    private static CorreoSaliente Nuevo(string destinatario, string? nombre, string asunto, string cuerpo, string tipo, int? creadoPor, string? lote) => new()
    {
        Destinatario = destinatario,
        NombreDestinatario = nombre,
        Asunto = asunto.Length > 200 ? asunto[..200] : asunto,
        Cuerpo = cuerpo,
        Tipo = tipo,
        Estado = EstadosCorreo.Pendiente,
        Lote = lote,
        CreadoPorUsuarioId = creadoPor,
        FechaCreacion = DateTime.UtcNow,
    };
}
