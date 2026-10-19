using System.ComponentModel.DataAnnotations;

namespace CapstonePsicologiaAPI.DTOs;

/// <summary>
/// Respuesta del login con usuario y contraseña. Con verificación en dos pasos no trae la sesión todavía:
/// trae un desafío y los canales por donde puede recibir el código.
/// </summary>
public class LoginResultadoDto
{
    public AdminSesionDto? Sesion { get; set; }
    public bool RequiereVerificacion { get; set; }
    public string? Desafio { get; set; }
    public List<CanalVerificacionDto> Canales { get; set; } = new();
}

public class CanalVerificacionDto
{
    /// <summary>"correo" o "sms".</summary>
    public string Canal { get; set; } = string.Empty;

    /// <summary>Destino oculto para mostrarlo (al***@gmail.com, *** *** 321).</summary>
    public string Destino { get; set; } = string.Empty;
}

public class CodigoSolicitudDto
{
    [Required]
    public string Desafio { get; set; } = string.Empty;

    [Required, RegularExpression("^(correo|sms)$", ErrorMessage = "Canal no válido")]
    public string Canal { get; set; } = string.Empty;
}

public class CodigoEnviadoDto
{
    public string Destino { get; set; } = string.Empty;
    public int ExpiraEnSegundos { get; set; }
}

public class CodigoVerificarDto
{
    [Required]
    public string Desafio { get; set; } = string.Empty;

    [Required, RegularExpression(@"^\d{6}$", ErrorMessage = "El código tiene 6 dígitos")]
    public string Codigo { get; set; } = string.Empty;
}
