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

        // Iniciativas de ejemplo, para que el sitio público no salga vacío; el administrador las edita.
        if (!await db.Iniciativas.AnyAsync())
        {
            db.Iniciativas.AddRange(
                new Iniciativa { Titulo = "Talleres educativos", Texto = "Espacios de aprendizaje para niñas, niños y adolescentes de la comunidad.", Icono = "📚", Color = ColoresIniciativa.Rojo, Orden = 1 },
                new Iniciativa { Titulo = "Actividades culturales", Texto = "Arte, música y tradiciones para fortalecer la identidad y las buenas costumbres.", Icono = "🎨", Color = ColoresIniciativa.Naranja, Orden = 2 },
                new Iniciativa { Titulo = "Acciones ambientales", Texto = "Campañas de cuidado del entorno con la participación de voluntarios.", Icono = "🌱", Color = ColoresIniciativa.Verde, Orden = 3 },
                new Iniciativa { Titulo = "Actividades animalistas", Texto = "Rescate, cuidado y sensibilización sobre el respeto a los animales.", Icono = "🐾", Color = ColoresIniciativa.Gris, Orden = 4 },
                new Iniciativa { Titulo = "Salud mental con valores", Texto = "Charlas y dinámicas para cuidar el bienestar emocional con valores y principios.", Icono = "💚", Color = ColoresIniciativa.Rojo, Orden = 5 },
                new Iniciativa { Titulo = "Voluntariado y prácticas", Texto = "Súmate como voluntario/a o practicante y ayuda a formar agentes de cambio.", Icono = "🤝", Color = ColoresIniciativa.Verde, Orden = 6 });
            await db.SaveChangesAsync();
        }

        if (await db.Usuarios.AnyAsync())
            return;

        var admin = new Usuario { NombreUsuario = "admin", Rol = Roles.SuperAdmin };
        admin.PasswordHash = hasher.HashPassword(admin, "123");
        db.Usuarios.Add(admin);
        await db.SaveChangesAsync();
    }
}
