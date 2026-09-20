using CapstonePsicologiaAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace CapstonePsicologiaAPI.Data;

/// <summary>Acceso a datos de las cuentas de usuario (tabla Usuarios).</summary>
public class UsuarioData : IUsuarioData
{
    private readonly AppDbContext _db;

    public UsuarioData(AppDbContext db)
    {
        _db = db;
    }

    public Task<bool> ExisteCorreoAsync(string correo)
    {
        return _db.Usuarios.AnyAsync(u => u.Correo == correo || u.NombreUsuario == correo);
    }

    public async Task CrearAsync(Usuario usuario)
    {
        _db.Usuarios.Add(usuario);
        await _db.SaveChangesAsync();
    }

    public Task<Usuario?> ObtenerPorIdAsync(int id)
    {
        return _db.Usuarios.FirstOrDefaultAsync(u => u.Id == id);
    }

    public Task<Usuario?> ObtenerPorCorreoAsync(string correo)
    {
        return _db.Usuarios.FirstOrDefaultAsync(u => u.Correo == correo);
    }

    public Task<bool> EstaActivoAsync(int id)
    {
        return _db.Usuarios.AsNoTracking().AnyAsync(u => u.Id == id && u.Activo);
    }

    public async Task ActualizarAsync(Usuario usuario)
    {
        _db.Usuarios.Update(usuario);
        await _db.SaveChangesAsync();
    }

    public Task<List<Usuario>> ListarAsync(string[] roles, string? buscar, bool? activo)
    {
        var consulta = _db.Usuarios.AsNoTracking().Where(u => roles.Contains(u.Rol));

        if (activo is not null)
            consulta = consulta.Where(u => u.Activo == activo);

        if (!string.IsNullOrWhiteSpace(buscar))
        {
            var texto = buscar.Trim();
            consulta = consulta.Where(u =>
                u.Nombre.Contains(texto) ||
                u.NombreUsuario.Contains(texto) ||
                (u.Correo != null && u.Correo.Contains(texto)));
        }

        return consulta.OrderByDescending(u => u.FechaRegistro).ToListAsync();
    }

    public Task<List<Usuario>> ObtenerDestinatariosAsync(string[] roles, bool respetarPreferencia)
    {
        return _db.Usuarios
            .AsNoTracking()
            .Where(u => roles.Contains(u.Rol) && u.Activo && u.Correo != null)
            .Where(u => !respetarPreferencia || u.Rol != Roles.Usuario || u.RecordatoriosCorreo)
            .OrderBy(u => u.Id)
            .ToListAsync();
    }
}
