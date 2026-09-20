namespace CapstonePsicologiaAPI.Services;

/// <summary>
/// Error esperado por una regla del negocio (dato inválido, sin permiso, no existe, ya ocupado).
/// Un middleware lo convierte en una respuesta HTTP con el código y el mensaje.
/// </summary>
public class ReglaNegocioException : Exception
{
    public int Codigo { get; }

    private ReglaNegocioException(int codigo, string mensaje) : base(mensaje)
    {
        Codigo = codigo;
    }

    public static ReglaNegocioException Invalida(string mensaje) => new(StatusCodes.Status400BadRequest, mensaje);
    public static ReglaNegocioException Prohibida(string mensaje) => new(StatusCodes.Status403Forbidden, mensaje);
    public static ReglaNegocioException NoEncontrada(string mensaje) => new(StatusCodes.Status404NotFound, mensaje);
    public static ReglaNegocioException Conflicto(string mensaje) => new(StatusCodes.Status409Conflict, mensaje);
}
