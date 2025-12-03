using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RFIDInventario.Server.Data;
using RFIDInventario.Server.Models;
using Microsoft.Data.SqlClient;

namespace RFIDInventario.Server.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class TagController : ControllerBase
    {
        private readonly AppDbContext _context;

        public TagController(AppDbContext context)
        {
            _context = context;
        }

        // insertar
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

        //get tags idTienda
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TagTienda>>> GetTags([FromQuery] string? idTienda = null)
        {
            if (string.IsNullOrEmpty(idTienda))
            {
                return await _context.TagTienda.ToListAsync();
            }

            var tags = await _context.TagTienda
                .Where(t => t.IdTienda == idTienda)
                .ToListAsync();

            return Ok(tags);
        }
    }
}
