using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using RFIDInventario.Server.Data;
using RFIDInventario.Server.Hubs;
using RFIDInventario.Server.Models;

namespace RFIDInventario.Server.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class TiendasController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IHubContext<NotificationHub> _hubContext;

        public TiendasController(AppDbContext context, IHubContext<NotificationHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Tienda>>> GetTiendas()
        {
            var tiendas = await _context.Tiendas.ToListAsync();
            return Ok(tiendas);
        }

        [HttpGet("{idTienda}")]
        public async Task<ActionResult<Tienda>> GetTienda(string idTienda)
        {
            var tienda = await _context.Tiendas.FindAsync(idTienda);
            if (tienda == null)
            {
                return NotFound();
            }
            return Ok(tienda);
        }

        [HttpPatch("{idTienda}/estado")]
        public async Task<IActionResult> CambiarEstado(string idTienda, [FromBody] string nuevoEstado)
        {
            if (string.IsNullOrWhiteSpace(nuevoEstado))
            {
                return BadRequest(new { Message = "El estado no puede estar vacío." });
            }

            nuevoEstado = nuevoEstado.Trim();
            var estadosValidos = new[] { "Abierto", "Cerrado" };

            if (!estadosValidos.Contains(nuevoEstado, StringComparer.OrdinalIgnoreCase))
            {
                return BadRequest(new
                {
                    Message = $"Estado '{nuevoEstado}' no es válido. Los valores permitidos son: Abierto, Cerrado."
                });
            }

            var tienda = await _context.Tiendas.FindAsync(idTienda);
            if (tienda == null)
            {
                return NotFound(new { Message = "La tienda no existe." });
            }

            tienda.Estado_Conteo = nuevoEstado;
            await _context.SaveChangesAsync();

            // envio notificaciones
            try
            {
                Console.WriteLine($"[SignalR] Enviando EstadoTiendaActualizado -> tienda={idTienda}, estado={nuevoEstado}");
                await _hubContext.Clients.Group(idTienda)
                    .SendAsync("EstadoTiendaActualizado", new { idTienda, estado = nuevoEstado });

                // Envío específico para Android (payload string) — mantiene compatibilidad
                if (string.Equals(nuevoEstado, "Cerrado", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine($"[SignalR] Enviando evento Cerrar al grupo {idTienda}");
                    await _hubContext.Clients.Group(idTienda).SendAsync("Cerrar", idTienda);
                    await _hubContext.Clients.All.SendAsync("Cerrar", idTienda);
                }
                else if (string.Equals(nuevoEstado, "Abierto", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine($"[SignalR] Enviando evento Iniciar al grupo {idTienda}");
                    await _hubContext.Clients.Group(idTienda).SendAsync("Iniciar", idTienda);
                    await _hubContext.Clients.All.SendAsync("Iniciar", idTienda);
                }

                // --- FALLBACK TEMPORAL
                //Console.WriteLine($"[SignalR] Envío fallback a Clients.All.Cerrar (DEBUG)");
                //await _hubContext.Clients.All.SendAsync("Cerrar", idTienda);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SignalR ERROR] al enviar notificación: {ex}");
                ///evito que se crash la api por notificacion
            }



            return Ok(new
            {
                Message = $"El estado de la tienda {idTienda} ha sido actualizado a '{nuevoEstado}'.",
                Tienda = tienda
            });
        }



        // swagger GET: /tiendas/{idTienda}/estado 
        [HttpGet("{idTienda}/estado")]
        public async Task<IActionResult> GetEstadoTienda(string idTienda)
        {
            var estado = await _context.Tiendas
                .Where(t => t.Codigo == idTienda)
                .Select(t => t.Estado_Conteo)
                .FirstOrDefaultAsync();

            if (estado == null)
            {
                return NotFound(new { Message = $"La tienda {idTienda} no existe." });
            }

            return Ok(new { Estado = estado });
        }
    }
}
