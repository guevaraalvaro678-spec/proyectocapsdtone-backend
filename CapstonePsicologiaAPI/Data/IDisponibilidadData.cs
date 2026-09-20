using CapstonePsicologiaAPI.Models;

namespace CapstonePsicologiaAPI.Data;

public interface IDisponibilidadData
{
    /// <summary>Bloques de un psicólogo, ordenados por día y hora.</summary>
    Task<List<Disponibilidad>> ListarDePsicologoAsync(int psicologoId, bool soloActivos);

    Task<Disponibilidad?> ObtenerPorIdAsync(int id);
    Task CrearAsync(Disponibilidad disponibilidad);
    Task ActualizarAsync(Disponibilidad disponibilidad);
    Task EliminarAsync(Disponibilidad disponibilidad);

    /// <summary>Ids de los psicólogos que tienen al menos un bloque activo.</summary>
    Task<HashSet<int>> ObtenerPsicologosConHorarioAsync();
}
