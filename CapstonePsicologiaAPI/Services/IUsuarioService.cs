using CapstonePsicologiaAPI.DTOs;

namespace CapstonePsicologiaAPI.Services;

public interface IUsuarioService
{
    /// <summary>Crea un usuario común y devuelve su sesión; null si el correo ya está registrado.</summary>

    Task<UsuarioPerfilDto?> ObtenerPerfilAsync(int usuarioId);

    Task<UsuarioPerfilDto?> ActualizarPerfilAsync(int usuarioId, UsuarioPerfilActualizarDto datos);

    /// <summary>
    /// Envía el enlace para restablecer la contraseña. Responde igual exista o no la cuenta,
    /// para que nadie pueda averiguar qué correos están registrados.
    /// </summary>
    Task SolicitarRecuperacionAsync(string correo);

    Task RestablecerPasswordAsync(RestablecerPasswordDto datos);

    // ---- Administración ----

    /// <summary>`tipo` es "Usuario" (pacientes) o "Administrador" (solo lo puede ver un SuperAdmin).</summary>
    Task<List<UsuarioListaDto>> ListarAsync(string tipo, string? buscar, bool? activo, string actorRol);

    Task<UsuarioDetalleDto> ObtenerDetalleAsync(int id, string actorRol);

    Task<UsuarioListaDto> CambiarActivoAsync(int id, bool activo, int actorId, string actorRol);
}
