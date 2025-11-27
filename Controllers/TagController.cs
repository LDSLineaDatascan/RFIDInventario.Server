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

        [HttpPost("{idTienda}")]
        public async Task<IActionResult> InsertTag(string idTienda, [FromBody] TagRequest request)
        {
            try
            {

                //validadcion de entrada
                if(string.IsNullOrWhiteSpace(request.Tag) || string.IsNullOrWhiteSpace(request.Ean))
                {
                    return BadRequest(new { Message = "Tag y EAN son obligatorios." });
                }

                //valido estado de la tienda
                var estado= await _context.Tiendas
                    .Where(t => t.Codigo == idTienda)
                    .Select(t => t.Estado)
                    .FirstOrDefaultAsync();

                if(!string.IsNullOrEmpty(estado) &&
                    (estado.Equals("Cerrar", StringComparison.OrdinalIgnoreCase) ||
                    estado.Equals("Cerrado", StringComparison.OrdinalIgnoreCase)))
                    {
                    //rechazo la escritura
                        return StatusCode(423, new { mensaje = "Tienda cerrada: no se permiten lecturas." });
                    }


                await _context.Database.ExecuteSqlRawAsync(
                    "EXEC INSERTAR_TAG_ACTUALIZADO_INVENTARIO @ID_TIENDA, @TAG, @EAN",
                    new SqlParameter("@ID_TIENDA", idTienda),
                    new SqlParameter("@TAG", request.Tag),
                    new SqlParameter("@EAN", request.Ean));

                Console.WriteLine("Procedimiento ejecutado correctamente.");
                return Ok(new { Message = "Procedimiento ejecutado correctamente." });
            }
            catch (Exception ex)
            {
                var errorResponse = new
                {
                    StatusCode = 400,
                    Error = "Error al procesar la solicitud",
                    Message = ex.Message
                };
                Console.WriteLine($"Error al ejecutar el procedimiento: {ex.Message}");
                return BadRequest(errorResponse);
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
    }
}
