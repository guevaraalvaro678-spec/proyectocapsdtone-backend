using CapstonePsicologiaAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace CapstonePsicologiaAPI.Data;

/// <summary>Acceso a datos del administrador (tabla Usuarios).</summary>
public class AdminData : IAdminData
{
    private readonly AppDbContext _db;

    public AdminData(AppDbContext db)
    {
        _db = db;
    }

    public Task<Usuario?> ObtenerPorNombreUsuarioAsync(string nombreUsuario)
    {
        return _db.Usuarios.FirstOrDefaultAsync(u => u.NombreUsuario == nombreUsuario);
    }

    public Task<bool> ExisteNombreUsuarioAsync(string nombreUsuario)
    {
        return _db.Usuarios.AnyAsync(u => u.NombreUsuario == nombreUsuario);
    }

    public async Task CrearAsync(Usuario usuario)
    {
        _db.Usuarios.Add(usuario);
        await _db.SaveChangesAsync();
    }
}
