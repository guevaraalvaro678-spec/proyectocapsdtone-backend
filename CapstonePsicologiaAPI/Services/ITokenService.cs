using CapstonePsicologiaAPI.Models;

namespace CapstonePsicologiaAPI.Services;

public interface ITokenService
{
    /// <summary>Genera un token JWT con el id, nombre y rol del usuario.</summary>
    string Generar(Usuario usuario);
}
