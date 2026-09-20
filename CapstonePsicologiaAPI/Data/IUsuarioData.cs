using CapstonePsicologiaAPI.Models;

namespace CapstonePsicologiaAPI.Data;

public interface IUsuarioData
{
    Task<bool> ExisteCorreoAsync(string correo);
    Task CrearAsync(Usuario usuario);
    Task<Usuario?> ObtenerPorIdAsync(int id);
    Task<Usuario?> ObtenerPorCorreoAsync(string correo);

    /// <summary>True si la cuenta existe y no está desactivada. Se consulta en cada petición con sesión.</summary>
    Task<bool> EstaActivoAsync(int id);
    Task ActualizarAsync(Usuario usuario);

    /// <summary>Cuentas de los roles indicados, con búsqueda por nombre o correo y filtro por estado.</summary>
    Task<List<Usuario>> ListarAsync(string[] roles, string? buscar, bool? activo);

    /// <summary>
    /// Cuentas activas con correo, para un envío masivo. Si `respetarPreferencia` es true, los usuarios
    /// comunes que apagaron los correos quedan fuera.
    /// </summary>
    Task<List<Usuario>> ObtenerDestinatariosAsync(string[] roles, bool respetarPreferencia);
}
