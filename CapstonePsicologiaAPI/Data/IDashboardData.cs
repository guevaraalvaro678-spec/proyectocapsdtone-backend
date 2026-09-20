namespace CapstonePsicologiaAPI.Data;

public interface IDashboardData
{
    Task<int> ContarUsuariosActivosAsync();
    Task<int> ContarPsicologosActivosAsync();
    Task<int> ContarCitasAsync(DateTime desde, DateTime hasta);
    Task<int> ContarSolicitudesPendientesAsync();
    Task<int> ContarMensajesSinLeerAsync();
    Task<int> ContarEventosPublicadosAsync();

    /// <summary>Inicio de todas las citas programadas o confirmadas entre las dos fechas.</summary>
    Task<List<DateTime>> ObtenerFechasDeCitasAsync(DateTime desde, DateTime hasta);
}
