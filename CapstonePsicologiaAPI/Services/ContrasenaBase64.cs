using System.Text;
using CapstonePsicologiaAPI.Models;
using Microsoft.AspNetCore.Identity;

namespace CapstonePsicologiaAPI.Services;

/// <summary>
/// Guarda las contraseñas codificadas en Base64 (decisión del equipo para el proyecto universitario).
/// Ojo: Base64 no es cifrado, se puede revertir; para producción habría que volver a <see cref="PasswordHasher{TUser}"/>.
/// Las cuentas creadas antes con el hash anterior (PBKDF2) siguen funcionando.
/// </summary>
public class ContrasenaBase64 : IPasswordHasher<Usuario>
{
    private readonly PasswordHasher<Usuario> _hashAnterior = new();

    public string HashPassword(Usuario usuario, string password) =>
        Convert.ToBase64String(Encoding.UTF8.GetBytes(password));

    public PasswordVerificationResult VerifyHashedPassword(Usuario usuario, string hashedPassword, string providedPassword)
    {
        if (hashedPassword == HashPassword(usuario, providedPassword))
            return PasswordVerificationResult.Success;

        // Cuentas antiguas con el hash PBKDF2 de ASP.NET Identity.
        try
        {
            return _hashAnterior.VerifyHashedPassword(usuario, hashedPassword, providedPassword) == PasswordVerificationResult.Failed
                ? PasswordVerificationResult.Failed
                : PasswordVerificationResult.SuccessRehashNeeded; // al entrar se vuelve a guardar en Base64
        }
        catch (FormatException)
        {
            return PasswordVerificationResult.Failed;
        }
    }
}
