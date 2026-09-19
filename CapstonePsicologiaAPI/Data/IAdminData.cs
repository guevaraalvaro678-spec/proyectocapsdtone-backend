using CapstonePsicologiaAPI.Models;

namespace CapstonePsicologiaAPI.Data;

public interface IAdminData
{
    Task<Usuario?> ObtenerPorNombreUsuarioAsync(string nombreUsuario);
    Task<bool> ExisteNombreUsuarioAsync(string nombreUsuario);
    Task CrearAsync(Usuario usuario);
}
