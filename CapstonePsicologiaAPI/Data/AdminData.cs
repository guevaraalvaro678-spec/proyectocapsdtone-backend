using CapstonePsicologiaAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace CapstonePsicologiaAPI.Data;

/// <summary>Acceso a datos de administradores y del login (tabla Usuarios).</summary>
public class AdminData : IAdminData
{
    private readonly AppDbContext _db;

    public AdminData(AppDbContext db)
    {
        _db = db;
    }

    public Task<Usuario?> ObtenerPorUsuarioOCorreoAsync(string valor)
    {
        return _db.Usuarios.FirstOrDefaultAsync(u => u.NombreUsuario == valor || u.Correo == valor);
    }

    public Task<bool> ExisteNombreUsuarioAsync(string nombreUsuario)
    {
        return _db.Usuarios.AnyAsync(u => u.NombreUsuario == nombreUsuario || u.Correo == nombreUsuario);
    }

    public async Task CrearAsync(Usuario usuario)
    {
        _db.Usuarios.Add(usuario);
        await _db.SaveChangesAsync();
    }

    public async Task RegistrarAccesoAsync(int usuarioId)
    {
        await _db.Usuarios
            .Where(u => u.Id == usuarioId)
            .ExecuteUpdateAsync(s => s.SetProperty(u => u.UltimoAcceso, DateTime.UtcNow));
    }
}
