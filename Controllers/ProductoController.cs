using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RFIDInventario.Server.Data;
using RFIDInventario.Server.Models;

namespace RFIDInventario.Server.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class ProductoController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ProductoController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("{idProducto}")]
        public async Task<IActionResult> GetProducto(string idProducto)
        {
            var producto = await _context.Productos.FindAsync(idProducto);
            return Ok(producto);
        }

        [HttpGet]
        public async Task<IActionResult> GetTodos()
        {
            var productos = await _context.Productos.ToListAsync();
            return Ok(productos);
        }
    }
}
