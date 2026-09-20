namespace CapstonePsicologiaAPI.Data;

/// <summary>Criterios para listar citas. Todo es opcional; las fechas son UTC.</summary>
public record CitaFiltro(
    string? Estado = null,
    int? PsicologoId = null,
    int? PacienteId = null,
    DateTime? Desde = null,
    DateTime? Hasta = null,
    int? Maximo = null);
