using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RFIDInventario.Server.Data;
using RFIDInventario.Server.Models;
using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.SignalR;
using RFIDInventario.Server.Hubs;

namespace RFIDInventario.Server.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class TagController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IHubContext<NotificationHub> _hubContext;

        public TagController(AppDbContext context, IHubContext<NotificationHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        [HttpPost("{idTienda}")]
        public async Task<IActionResult> InsertTag(string idTienda, [FromBody] TagRequest request)
        {
            try
            {
                //VALIDACION ENTRADA
                if (request == null)
                    return BadRequest(new { Message = "Solicitud inválida, no se recibió JSON." });

                if (string.IsNullOrWhiteSpace(request.Tag) ||
                    string.IsNullOrWhiteSpace(request.Ean))
                {
                    return BadRequest(new { Message = "Tag y EAN son obligatorios." });
                }

                // -------- VALIDAR EXISTENCIA DE TIENDA --------
                var estado = await _context.Tiendas
                    .Where(t => t.Codigo == idTienda)
                    .Select(t => t.Estado_Conteo)
                    .FirstOrDefaultAsync();

                if (estado == null)
                {
                    return NotFound(new { Message = $"La tienda {idTienda} no existe." });
                }

                // -------- VALIDAR ESTADO --------
                if (estado.Equals("Cerrado", StringComparison.OrdinalIgnoreCase))
                {
                    return StatusCode(423, new { Message = "Tienda cerrada: no se permiten lecturas." });
                }

                // -------- EJECUTAR PROCEDIMIENTO --------
                await _context.Database.ExecuteSqlRawAsync(
                    "EXEC INSERTAR_TAG_ACTUALIZADO_INVENTARIO @ID_TIENDA, @TAG, @EAN",
                    new SqlParameter("@ID_TIENDA", idTienda),
                    new SqlParameter("@TAG", request.Tag),
                    new SqlParameter("@EAN", request.Ean)
                );
                //actualizar adicionales
                await _hubContext.Clients.All.SendAsync("InventarioActualizado", idTienda);

                Console.WriteLine($"TAG registrado correctamente: TAG={request.Tag}, EAN={request.Ean}, tienda={idTienda}");

                return Ok(new { Message = "TAG registrado correctamente." });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al insertar TAG: {ex.Message}");

                return StatusCode(500, new
                {
                    Message = "Error al procesar la solicitud.",
                    Detail = ex.Message
                });
            }
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<TagTienda>>> GetTags([FromQuery] string? idTienda=null)
        {
            if(string.IsNullOrEmpty(idTienda))
            {
                return await _context.TagTienda.ToListAsync();
            }
            var tags= await _context.TagTienda
                .Where(t => t.IdTienda == idTienda)
                .ToListAsync();

            return Ok(tags);
        }

        [HttpGet("por-ean/{idTienda}/{ean}")]
        public async Task<IActionResult> GetTagsPorEan(string idTienda, string ean)
        {
            if (string.IsNullOrWhiteSpace(ean))
                return BadRequest(new { Message = "EAN requerido." });

            ean = ean.Trim();
            idTienda = idTienda.Trim();

            var resultado = await (
                from tag in _context.TagTienda
                join prod in _context.Productos
                    on tag.Ean.Trim() equals prod.Codigo.Trim()
                where tag.IdTienda.Trim() == idTienda
                      && tag.Ean.Trim() == ean
                select new
                {
                    epc = tag.Tag,
                    nombreProducto = prod.Nombre
                }
            )
            .Distinct()
            .ToListAsync();

            if (!resultado.Any())
                return NotFound(new { Message = "No se encontraron tags para este EAN." });

            return Ok(resultado);
        }
    }
}
