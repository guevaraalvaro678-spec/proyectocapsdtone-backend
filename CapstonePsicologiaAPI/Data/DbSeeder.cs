using CapstonePsicologiaAPI.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CapstonePsicologiaAPI.Data;

public static class DbSeeder
{
    /// <summary>Crea el usuario admin (admin / 123, rol SuperAdmin) si todavía no existe ningún usuario.</summary>
    public static async Task SeedAsync(IServiceProvider services)
    {
        var db = services.GetRequiredService<AppDbContext>();
        var hasher = services.GetRequiredService<IPasswordHasher<Usuario>>();

        if (await db.Usuarios.AnyAsync())
            return;

        var admin = new Usuario { NombreUsuario = "admin", Rol = Roles.SuperAdmin };
        admin.PasswordHash = hasher.HashPassword(admin, "123");
        db.Usuarios.Add(admin);
        await db.SaveChangesAsync();
    }
}
