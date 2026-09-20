namespace CapstonePsicologiaAPI.Services;

/// <summary>
/// Proceso en segundo plano: cada pocos segundos envía los correos pendientes y, cada minuto,
/// revisa qué citas necesitan recordatorio. No hace nada mientras el SMTP no esté configurado.
/// </summary>
public class EnvioCorreoWorker : BackgroundService
{
    private static readonly TimeSpan PausaEntreRevisiones = TimeSpan.FromSeconds(3);
    private static readonly TimeSpan PausaEntreRecordatorios = TimeSpan.FromMinutes(1);

    private readonly IServiceScopeFactory _scopes;
    private readonly ILogger<EnvioCorreoWorker> _logger;

    public EnvioCorreoWorker(IServiceScopeFactory scopes, ILogger<EnvioCorreoWorker> logger)
    {
        _scopes = scopes;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        var ultimoRecordatorio = DateTime.MinValue;

        while (!ct.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopes.CreateScope();
                var correos = scope.ServiceProvider.GetRequiredService<ICorreoService>();

                await correos.ProcesarPendientesAsync(ct);

                if (correos.SmtpConfigurado && DateTime.UtcNow - ultimoRecordatorio >= PausaEntreRecordatorios)
                {
                    await scope.ServiceProvider.GetRequiredService<ICitaService>().ProcesarRecordatoriosAsync();
                    ultimoRecordatorio = DateTime.UtcNow;
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                // Un fallo (por ejemplo, la base sin conexión) no debe tumbar la API.
                _logger.LogError(ex, "Error en el proceso de envío de correos");
            }

            try
            {
                await Task.Delay(PausaEntreRevisiones, ct);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }
}
