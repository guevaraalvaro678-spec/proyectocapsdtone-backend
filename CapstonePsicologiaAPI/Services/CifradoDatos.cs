using System.Security.Cryptography;
using System.Text;

namespace CapstonePsicologiaAPI.Services;

/// <summary>
/// Cifrado en la base de los datos personales sensibles (HU-20), con AES-GCM.
/// Se guarda como "enc:" + Base64(nonce | etiqueta | texto cifrado). Un valor sin ese prefijo es un dato
/// anterior al cifrado y se devuelve tal cual (al iniciar, la API cifra los que queden).
/// </summary>
public static class CifradoDatos
{
    public const string Prefijo = "enc:";
    private const int BytesNonce = 12;
    private const int BytesEtiqueta = 16;

    private static byte[]? _clave;

    /// <summary>La clave AES de 256 bits sale del secreto de configuración (Seguridad:ClaveCifrado).</summary>
    public static void Configurar(string secreto) => _clave = SHA256.HashData(Encoding.UTF8.GetBytes(secreto));

    public static string Cifrar(string texto)
    {
        if (string.IsNullOrEmpty(texto) || texto.StartsWith(Prefijo))
            return texto;

        var plano = Encoding.UTF8.GetBytes(texto);
        var salida = new byte[BytesNonce + BytesEtiqueta + plano.Length];
        var nonce = salida.AsSpan(0, BytesNonce);
        RandomNumberGenerator.Fill(nonce);

        using var aes = new AesGcm(Clave, BytesEtiqueta);
        aes.Encrypt(nonce, plano, salida.AsSpan(BytesNonce + BytesEtiqueta), salida.AsSpan(BytesNonce, BytesEtiqueta));
        return Prefijo + Convert.ToBase64String(salida);
    }

    public static string Descifrar(string valor)
    {
        if (string.IsNullOrEmpty(valor) || !valor.StartsWith(Prefijo))
            return valor;

        var datos = Convert.FromBase64String(valor[Prefijo.Length..]);
        var plano = new byte[datos.Length - BytesNonce - BytesEtiqueta];

        using var aes = new AesGcm(Clave, BytesEtiqueta);
        aes.Decrypt(datos.AsSpan(0, BytesNonce), datos.AsSpan(BytesNonce + BytesEtiqueta), datos.AsSpan(BytesNonce, BytesEtiqueta), plano);
        return Encoding.UTF8.GetString(plano);
    }

    private static byte[] Clave =>
        _clave ?? throw new InvalidOperationException("Falta configurar la clave de cifrado (Seguridad:ClaveCifrado)");
}
