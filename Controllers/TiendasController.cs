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

        public TiendasController(AppDbContext context)
        {
            _context = context;
        }


        //Administrador
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Tienda>>> GetTiendas()
        {
            var tiendas = await _context.Tiendas.ToListAsync();
            return Ok(tiendas);
        }

        //USer
        [HttpGet("usuario/{usuarioId}")]
        public async Task<ActionResult<IEnumerable<Tienda>>> GetTiendasPorUsuario(int usuarioId)
        {
            var tiendas = await (
                from ut in _context.UsuariosTiendas
                join t in _context.Tiendas on ut.TiendaCodigo equals t.Codigo
                where ut.UsuarioId == usuarioId
                select t
            ).ToListAsync();

            if (!tiendas.Any())
            {
                return NotFound();
            }

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

        //editar estadod de tienda
        /* [HttpPut("{idTienda}/estado")]
         public async Task<IActionResult> CambiarEstadoTienda(string idTienda, [FromBody] EstadoRequest request, [FromServices] IHubContext<NotificationHub> hubContext)
         {
             var tienda = await _context.Tiendas.FindAsync(idTienda);
             if (tienda == null) return NotFound(new { mensaje = "Tienda no encontrada" });

             tienda.Estado = request.Estado;
             await _context.SaveChangesAsync();

             //notifico al grupo de tienda correspondiente
             if (//request.Estado?.Equals("Cerrar", StringComparison.OrdinalIgnoreCase) == true ||
                 request.Estado?.Equals("Cerrado", StringComparison.OrdinalIgnoreCase) == true)
             {
                 //await hubContext.Clients.Group(idTienda).SendAsync("Cerrar", idTienda);
                 await hubContext.Clients.All.SendAsync("Cerrado", idTienda);
             }
             else
             {
                 //await hubContext.Clients.Group(idTienda).SendAsync("Reiniciar", idTienda);
                 await hubContext.Clients.All.SendAsync("Reiniciar", idTienda);
             }

             //notificacion signalR de estado de tienda
             await hubContext.Clients.All.SendAsync("EstadoTiendaActualizado", 
                 new { idTienda, estado = request.Estado });

             return Ok(new { mensaje = "Estado actualizado" });
         }*/

        [HttpPut("{idTienda}/estado")]
            public async Task<IActionResult> CambiarEstadoTienda(
            string idTienda,
            [FromBody] EstadoRequest request,
            [FromServices] IHubContext<NotificationHub> hubContext)
        {
            // Normalizo y valido estado
            var estadoRecibido = request?.Estado?.Trim();
            if (string.IsNullOrEmpty(estadoRecibido) ||
                !(estadoRecibido.Equals("Cerrado", StringComparison.OrdinalIgnoreCase)
                  || estadoRecibido.Equals("Abierto", StringComparison.OrdinalIgnoreCase)))
            {
                return BadRequest(new { mensaje = "Estado inválido. Valores permitidos: 'Cerrado' o 'Abierto'." });
            }

            var tienda = await _context.Tiendas.FindAsync(idTienda);
            if (tienda == null) return NotFound(new { mensaje = "Tienda no encontrada" });

            // Guardo estado en BD
            tienda.Estado = estadoRecibido;
            await _context.SaveChangesAsync();

            //evento principal a Android
            //await hubContext.Clients.All.SendAsync("EstadoTiendaActualizado", new { idTienda, estado = estadoRecibido });
            await hubContext.Clients.Group(idTienda)
                .SendAsync("EstadoTiendaActualizado", new { idTienda, estado = estadoRecibido });


            //sugerencia
            //await hubContext.Clients.All.SendAsync("EstadoTiendaActuaizado");
            Console.WriteLine("**************Evento EstadoTiendaActuaizado enviado para prueba automatica");
            Console.WriteLine($"Estado de tienda {idTienda} actualizado a {estadoRecibido}");

            return Ok(new { mensaje = "Estado actualizado" });
        }

        public class EstadoRequest
        {
            public string Estado { get; set; } = string.Empty;
        }

    }
}
