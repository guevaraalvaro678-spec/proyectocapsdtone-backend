using CapstonePsicologiaAPI.Data;
using CapstonePsicologiaAPI.DTOs;
using Microsoft.EntityFrameworkCore;

namespace CapstonePsicologiaAPI.Services;

public interface IRespaldoService
{
    Task<RespaldoEstadoDto> EstadoAsync();
    Task<RespaldoDto> RespaldarAsync();

    /// <summary>Hace una copia si la última tiene más de las horas configuradas (lo llama el proceso automático).</summary>
    Task<RespaldoDto?> RespaldarSiTocaAsync();
}

/// <summary>
/// Copias de seguridad de la base (HU-22). En SQL Server se hace BACKUP ... COPY_ONLY (no altera la cadena de copias)
/// a una carpeta y se conservan las últimas N; en Azure SQL no hace falta porque Azure guarda copias automáticas.
/// </summary>
public class RespaldoService : IRespaldoService
{
    private const int EdicionAzureSql = 5;

    private readonly AppDbContext _db;
    private readonly IConfiguration _config;

    public RespaldoService(AppDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    private string Carpeta =>
        string.IsNullOrWhiteSpace(_config["Respaldo:Carpeta"])
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Respaldos AMAAC")
            : _config["Respaldo:Carpeta"]!;

    private int HorasEntreCopias => Math.Max(1, _config.GetValue("Respaldo:HorasEntreCopias", 24));
    private int CopiasAConservar => Math.Max(1, _config.GetValue("Respaldo:CopiasAConservar", 7));

    public async Task<RespaldoEstadoDto> EstadoAsync()
    {
        var azure = await EsAzureAsync();
        var copias = azure ? new List<RespaldoDto>() : Listar();
        return new RespaldoEstadoDto
        {
            EnAzure = azure,
            Carpeta = azure ? null : Carpeta,
            HorasEntreCopias = HorasEntreCopias,
            CopiasAConservar = CopiasAConservar,
            Copias = copias,
            ProximaCopia = azure ? null : (copias.FirstOrDefault()?.Fecha ?? DateTime.UtcNow).AddHours(copias.Count == 0 ? 0 : HorasEntreCopias),
        };
    }

    public async Task<RespaldoDto?> RespaldarSiTocaAsync()
    {
        if (await EsAzureAsync())
            return null;

        var ultima = Listar().FirstOrDefault();
        return ultima is not null && ultima.Fecha > DateTime.UtcNow.AddHours(-HorasEntreCopias) ? null : await RespaldarAsync();
    }

    public async Task<RespaldoDto> RespaldarAsync()
    {
        if (await EsAzureAsync())
            throw ReglaNegocioException.Invalida("La base está en Azure SQL: Azure ya guarda copias automáticas");

        Directory.CreateDirectory(Carpeta);
        var nombreBase = _db.Database.GetDbConnection().Database;
        var ruta = Path.Combine(Carpeta, $"{nombreBase}_{DateTime.UtcNow.AddHours(-5):yyyy-MM-dd_HHmmss}.bak");

        _db.Database.SetCommandTimeout(TimeSpan.FromMinutes(5));
        try
        {
            await _db.Database.ExecuteSqlAsync($"BACKUP DATABASE {nombreBase} TO DISK = {ruta} WITH COPY_ONLY, INIT");
        }
        catch (Microsoft.Data.SqlClient.SqlException ex)
        {
            throw ReglaNegocioException.Invalida($"No se pudo crear la copia en {Carpeta}: {ex.Message}");
        }

        // Solo se conservan las más recientes.
        foreach (var vieja in Archivos().Skip(CopiasAConservar))
            vieja.Delete();

        return ADto(new FileInfo(ruta));
    }

    private List<RespaldoDto> Listar() => Archivos().Select(ADto).ToList();

    private IEnumerable<FileInfo> Archivos() =>
        Directory.Exists(Carpeta)
            ? new DirectoryInfo(Carpeta).GetFiles("*.bak").OrderByDescending(f => f.LastWriteTimeUtc)
            : Enumerable.Empty<FileInfo>();

    private static RespaldoDto ADto(FileInfo f) => new()
    {
        Nombre = f.Name,
        Fecha = DateTime.SpecifyKind(f.LastWriteTimeUtc, DateTimeKind.Utc),
        TamanoBytes = f.Exists ? f.Length : 0,
    };

    private async Task<bool> EsAzureAsync() =>
        await _db.Database.SqlQuery<int>($"SELECT CAST(SERVERPROPERTY('EngineEdition') AS int) AS [Value]").SingleAsync() == EdicionAzureSql;
}

/// <summary>Copia automática: revisa cada hora y respalda cuando pasó el intervalo configurado.</summary>
public class RespaldoWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopes;
    private readonly IConfiguration _config;
    private readonly ILogger<RespaldoWorker> _logger;

    public RespaldoWorker(IServiceScopeFactory scopes, IConfiguration config, ILogger<RespaldoWorker> logger)
    {
        _scopes = scopes;
        _config = config;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_config.GetValue("Respaldo:Automatico", true))
            return;

        try
        {
            // Deja que la API termine de arrancar antes de la primera revisión.
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopes.CreateScope();
                    var copia = await scope.ServiceProvider.GetRequiredService<IRespaldoService>().RespaldarSiTocaAsync();
                    if (copia is not null)
                        _logger.LogInformation("Copia de seguridad creada: {Nombre}", copia.Nombre);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogWarning(ex, "No se pudo crear la copia de seguridad automática");
                }

                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            // La API se está deteniendo.
        }
    }
}
