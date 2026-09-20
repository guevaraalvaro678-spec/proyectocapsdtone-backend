using CapstonePsicologiaAPI.Models;

namespace CapstonePsicologiaAPI.Data;

public interface IColaboradorData
{
    Task<Psicologo?> ObtenerPorIdAsync(int id);
    Task<Psicologo?> ObtenerPorCorreoAsync(string correo);
    Task<Psicologo?> ObtenerPorUsuarioIdAsync(int usuarioId);
    Task<List<Psicologo>> ListarAsync(string? estado);
    Task CrearAsync(Psicologo psicologo);
    Task GuardarAsync();
}
