using RFIDInventario.Server.Data;
using RFIDInventario.Server.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using RFIDInventario.Server.Hubs;

namespace RFIDInventario.Server.Services
{
    public class UsuarioTiendaService
    {
        private readonly AppDbContext _context;
        private readonly IHubContext<NotificationHub> _hubContext;

        public UsuarioTiendaService(AppDbContext context, IHubContext<NotificationHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        // 🔹 Obtener las tiendas de un usuario, incluyendo correos
        public async Task<List<object>> ObtenerTiendasPorUsuario(int usuarioId)
        {
            return await _context.UsuariosTiendas
                .Where(x => x.UsuarioId == usuarioId)
                .Include(x => x.Usuario)
                .Include(x => x.AsignadoPor)
                .Select(x => new
                {
                    x.Id,
                    x.UsuarioId,
                    x.TiendaCodigo,
                    x.RolAsignado,
                    x.FechaAsignacion,
                    x.AsignadoPorId,
                    Usuario = x.Usuario != null ? new { x.Usuario.Id, x.Usuario.Correo } : null,
                    AsignadoPor = x.AsignadoPor != null ? new { x.AsignadoPor.Id, x.AsignadoPor.Correo } : null
                })
                .ToListAsync<object>();
        }


        // 🔹 Asignar tienda a un usuario
        public async Task<object> AsignarTienda(int usuarioId, string tiendaCodigo, string rol, int? asignadoPorId)
        {
            var asignacion = new UsuariosTiendas
            {
                UsuarioId = usuarioId,
                TiendaCodigo = tiendaCodigo,
                RolAsignado = rol,
                FechaAsignacion = DateTime.Now,
                AsignadoPorId = asignadoPorId
            };

            _context.UsuariosTiendas.Add(asignacion);
            await _context.SaveChangesAsync();

            // Correos para respuesta
            var usuario = await _context.Usuarios.FindAsync(usuarioId);
            var asignador = asignadoPorId.HasValue ? await _context.Usuarios.FindAsync(asignadoPorId) : null;

            var result = new
            {
                asignacion.Id,
                asignacion.UsuarioId,
                asignacion.TiendaCodigo,
                asignacion.RolAsignado,
                asignacion.FechaAsignacion,
                asignacion.AsignadoPorId,
                UsuarioCorreo = usuario?.Correo,
                AsignadoPorCorreo = asignador?.Correo
            };

            await _hubContext.Clients.All.SendAsync("TiendaAsignada", result);
            return result;
        }

        // 🔹 Eliminar asignación por ID
        public async Task<bool> EliminarAsignacion(int id)
        {
            var asignacion = await _context.UsuariosTiendas.FindAsync(id);
            if (asignacion == null)
                return false;

            _context.UsuariosTiendas.Remove(asignacion);
            await _context.SaveChangesAsync();

            await _hubContext.Clients.All.SendAsync("TiendaDesasignada", id);
            return true;
        }

        // 🔹 Eliminar asignación por correo (y opcionalmente tienda)
        public async Task<bool> EliminarAsignacionPorCorreo(string correoUsuario, string? tiendaCodigo = null)
        {
            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Correo == correoUsuario);
            if (usuario == null)
                return false;

            var query = _context.UsuariosTiendas.Where(x => x.UsuarioId == usuario.Id);

            if (!string.IsNullOrEmpty(tiendaCodigo))
                query = query.Where(x => x.TiendaCodigo == tiendaCodigo);

            var asignaciones = await query.ToListAsync();
            if (!asignaciones.Any())
                return false;

            _context.UsuariosTiendas.RemoveRange(asignaciones);
            await _context.SaveChangesAsync();

            // Notificar todas las eliminaciones
            foreach (var asignacion in asignaciones)
                await _hubContext.Clients.All.SendAsync("TiendaDesasignada", asignacion.Id);

            return true;
        }
    }
}
