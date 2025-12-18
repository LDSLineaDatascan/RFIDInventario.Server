using RFIDInventario.Server.Data;
using RFIDInventario.Server.Models;
using Microsoft.EntityFrameworkCore;

namespace RFIDInventario.Server.Services
{
    public class UsuarioService
    {
        private readonly AppDbContext _context;

        public UsuarioService(AppDbContext context)
        {
            _context = context;
        }

        // Obtener todos los usuarios
        public async Task<List<Usuario>> GetAllUsuariosAsync()
        {
            return await _context.Usuarios.ToListAsync();
        }

        // Buscar usuario por ID
        public async Task<Usuario?> GetUsuarioByIdAsync(int id)
        {
            return await _context.Usuarios.FindAsync(id);
        }

        // Buscar usuario por correo
        public async Task<Usuario?> GetUsuarioByCorreoAsync(string correo)
        {
            return await _context.Usuarios.FirstOrDefaultAsync(u => u.Correo == correo);
        }

        // Crear usuario
        public async Task<Usuario> AddUsuarioAsync(Usuario usuario)
        {
            _context.Usuarios.Add(usuario);
            await _context.SaveChangesAsync();
            return usuario;
        }

        // Actualizar usuario
        public async Task<bool> UpdateUsuarioAsync(Usuario usuario)
        {
            var existingUsuario = await _context.Usuarios.FindAsync(usuario.Id);
            if (existingUsuario == null) return false;

            existingUsuario.Correo = usuario.Correo;
            existingUsuario.Nombre = usuario.Nombre;
            existingUsuario.Rol = usuario.Rol;
            existingUsuario.Activo = usuario.Activo;
            existingUsuario.FechaActualizado = DateTime.Now;

            await _context.SaveChangesAsync();
            return true;
        }

        // Eliminar usuario
        public async Task<bool> DeleteUsuarioAsync(int id)
        {
            var usuario = await _context.Usuarios.FindAsync(id);
            if (usuario == null) return false;

            _context.Usuarios.Remove(usuario);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
