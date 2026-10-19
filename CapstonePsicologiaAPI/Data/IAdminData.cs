using CapstonePsicologiaAPI.Models;

namespace CapstonePsicologiaAPI.Data;

public interface IAdminData
{
    /// <summary>Busca por nombre de usuario o por correo (así también entran usuarios y psicólogos).</summary>
    Task<Usuario?> ObtenerPorUsuarioOCorreoAsync(string valor);

    Task<bool> ExisteNombreUsuarioAsync(string nombreUsuario);
    Task CrearAsync(Usuario usuario);

    /// <summary>Anota la fecha del último inicio de sesión y limpia los intentos fallidos.</summary>
    Task RegistrarAccesoAsync(int usuarioId);
    Task RegistrarIntentoFallidoAsync(int usuarioId, int intentos, DateTime? bloqueadoHasta);
    Task ActualizarPasswordAsync(int usuarioId, string passwordHash);
}
