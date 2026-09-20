using CapstonePsicologiaAPI.DTOs;

namespace CapstonePsicologiaAPI.Services;

public interface IDashboardService
{
    Task<DashboardDto> ObtenerAsync();
}
