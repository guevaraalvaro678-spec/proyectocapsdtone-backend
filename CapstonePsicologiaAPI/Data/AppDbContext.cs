using Microsoft.EntityFrameworkCore;

namespace CapstonePsicologiaAPI.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    // Aquí se agregarán los DbSet de cada módulo (Pacientes, Citas, etc.)
}
