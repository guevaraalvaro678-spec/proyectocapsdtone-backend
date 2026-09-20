namespace CapstonePsicologiaAPI.DTOs;

/// <summary>Resumen real del sistema para el dashboard del administrador.</summary>
public class DashboardDto
{
    public int UsuariosActivos { get; set; }
    public int PsicologosActivos { get; set; }
    public int CitasHoy { get; set; }
    public int CitasProximos7Dias { get; set; }
    public int SolicitudesPendientes { get; set; }
    public int MensajesSinLeer { get; set; }
    public int EventosPublicados { get; set; }
    public int CorreosPendientes { get; set; }

    /// <summary>Citas de los próximos 7 días (hora de Lima), una entrada por día.</summary>
    public List<CitasPorDiaDto> CitasPorDia { get; set; } = new();

    public List<CitaDto> ProximasCitas { get; set; } = new();
}

public class CitasPorDiaDto
{
    /// <summary>Fecha en formato yyyy-MM-dd (hora de Lima).</summary>
    public string Fecha { get; set; } = string.Empty;
    public int Cantidad { get; set; }
}
