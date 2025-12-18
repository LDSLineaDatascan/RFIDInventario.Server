using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RFIDInventario.Server.Data;
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
    }
}
