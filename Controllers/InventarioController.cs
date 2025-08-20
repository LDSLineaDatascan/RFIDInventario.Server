using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using RFIDInventario.Server.Data;
using RFIDInventario.Server.Hubs;
using RFIDInventario.Server.Services;
using RFIDInventario.Server.Models;


namespace RFIDInventario.Server.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class InventarioController(AppDbContext context) : Controller
    {
        private readonly AppDbContext _context = context;
        

        [HttpGet("{idTienda}")]
        public async Task<IActionResult> GetProductosPorTienda(string idTienda)
        {
            var query = from inv in _context.InventarioTeorico
                        join tie in _context.Tiendas on inv.IdTienda equals tie.Codigo
                        join pro in _context.Productos on inv.IdProducto equals pro.Codigo
                        join inv2 in _context.InventarioFisico on new { inv.IdProducto, inv.IdTienda }
                        equals new { inv2.IdProducto, inv2.IdTienda } into inv2Group
                        from inv2 in inv2Group.DefaultIfEmpty()
                        where inv.IdTienda == idTienda
                        select new
                        {
                            inv.IdTienda,
                            Tienda = tie.Nombre,
                            inv.IdProducto,
                            Producto = pro.Nombre,
                            pro.Categoria,
                            inv.Cantidad,
                            CantidadLectura = inv2 != null ? inv2.CantidadLeida : 0  // usoo operador ternario para manejar nulos
                        };

            var productosConInventario = await query.ToListAsync();

            return Ok(productosConInventario);
        }

        [HttpGet("fisico/{idTienda}")]
        public async Task<IActionResult> GetInventarioFisicoPorTienda(string idTienda)
        {
            var query = from inv in _context.InventarioFisico
                        join pro in _context.Productos on inv.IdProducto equals pro.Codigo
                        join tie in _context.Tiendas on inv.IdTienda equals tie.Codigo
                        where inv.IdTienda == idTienda
                        select new
                        {
                            inv.IdTienda,
                            Tienda = tie.Nombre,
                            inv.IdProducto,
                            Producto = pro.Nombre,
                            pro.Categoria,
                            CantidadLeida = inv.CantidadLeida
                        };

            var inventarioFisico = await query.ToListAsync();

            return Ok(inventarioFisico);
        }

        [HttpGet("comparacion/{idTienda}")]
        public async Task<IActionResult> GetComparacionInventario(string idTienda)
        {
            var teoricoQuery = from inv in _context.InventarioTeorico
                               where inv.IdTienda == idTienda
                               join pro in _context.Productos on inv.IdProducto equals pro.Codigo 
                               //nuevo
                               into gj from pro in gj.DefaultIfEmpty()
                               select new
                               {
                                   inv.IdProducto,
                                   pro.Nombre,
                                   pro.Categoria,
                                   StockTeorico = inv.Cantidad,
                                   StockFisico = 0
                               };

            var fisicoQuery = from inv in _context.InventarioFisico
                              where inv.IdTienda == idTienda
                              join pro in _context.Productos on inv.IdProducto equals pro.Codigo
                              select new
                              {
                                  inv.IdProducto,
                                  pro.Nombre,
                                  pro.Categoria,
                                  StockTeorico = 0,
                                  StockFisico = inv.CantidadLeida
                              };

            var union = await teoricoQuery
                .Union(fisicoQuery)
                .ToListAsync();

            var resultado = union
                .GroupBy(x => new { x.IdProducto, x.Nombre, x.Categoria })
                .Select(g =>
                {
                    var teorico = g.Sum(x => x.StockTeorico);
                    var fisico = g.Sum(x => x.StockFisico);
                    return new
                    {
                        g.Key.IdProducto,
                        g.Key.Nombre,
                        g.Key.Categoria,
                        StockTeorico = teorico,
                        StockFisico = fisico,
                        Diferencia = fisico - teorico,
                        Estado = teorico == 0 && fisico > 0 ? "Adicional":
                                 fisico == teorico ? "Coincide" :
                                 fisico < teorico ? "Faltante" : "Sobrante",
                                 
                        Adicionales =teorico == 0 && fisico > 0 ? fisico: 0
                    };
                }).ToList();

            return Ok(resultado);
        }


        [HttpGet("detalleCategoria/{idTienda}/{categoria}")]
        public async Task<IActionResult> GetDetalleCategoria(string idTienda, string categoria)
        {
            var query = from inv in _context.InventarioTeorico
                        join tie in _context.Tiendas on inv.IdTienda equals tie.Codigo
                        join pro in _context.Productos on inv.IdProducto equals pro.Codigo
                        join inv2 in _context.InventarioFisico
                             on new { inv.IdProducto, inv.IdTienda }
                             equals new { inv2.IdProducto, inv2.IdTienda } into inv2Group
                        from inv2 in inv2Group.DefaultIfEmpty()
                        where inv.IdTienda == idTienda && pro.Categoria == categoria
                        select new
                        {
                            inv.IdTienda,
                            Tienda = tie.Nombre,
                            inv.IdProducto,
                            Producto = pro.Nombre,
                            pro.Categoria,
                            StockTeorico = inv.Cantidad,
                            StockFisico = inv2 != null ? inv2.CantidadLeida : 0
                        };

            var productos = await query.ToListAsync();

            return Ok(productos);
        }

        [HttpPost("cargarTeorico")]
        public async Task<IActionResult> CargarInventarioTeorico([FromServices] IHubContext<NotificationHub> hubContext)
        {
            try
            {

                await _context.Database.ExecuteSqlRawAsync("EXEC CARGAR_INVENTARIO_TEORICO");
                
                //Notificar a todos los clientes que el inventario ha cambiado
                await hubContext.Clients.All.SendAsync("InventarioActualizado");


                return Ok(new { mensaje = "Inventario teórico cargado correctamente" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al cargar inventario teórico", error = ex.Message });
            }
        }

        ///********************************reinicio por signalR***************************************/
        /*[HttpPost("reiniciar")]
        public async Task<IActionResult> ReiniciarInventario()
        {
            try
            {
                await _context.Database.ExecuteSqlRawAsync("EXEC ReiniciarInventario");
                return Ok(new { mensaje = "Inventario reiniciado correctamente." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }*/


        /*[HttpPost("reiniciar")]
        public async Task<IActionResult> ReiniciarInventario([FromServices] IHubContext<NotificationHub> hubContext)
        {
            try
            {
                string idTienda = "T001"; 



                // 1. Ejecuta la lógica real de reinicio
                var inventarioService = new InventarioService(_context);
                inventarioService.Reiniciar(idTienda);

                // 2. Notifica a todos los clientes mediante SignalR
                await hubContext.Clients.All.SendAsync("Reiniciar", idTienda);

                return Ok(new { mensaje = "Inventario reiniciado correctamente desde SignalR." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }*/

        [HttpPost("reiniciar")]
        public async Task<IActionResult> ReiniciarInventario(
            [FromBody] ReinicioRequest request,
            [FromServices] IHubContext<NotificationHub> hubContext)
        {
                try
                {
                    string idTienda = request.IdTienda;
                    var inventarioService = new InventarioService(_context);
                    inventarioService.Reiniciar(idTienda);

                    await hubContext.Clients.All.SendAsync("Reiniciar", idTienda);

                    return Ok(new { mensaje = $"Inventario reiniciado para tienda {idTienda}" });
                }
                catch (Exception ex)
                {
                    return StatusCode(500, new { error = ex.Message });
                }
        }






        /*[HttpPost("inventario/reiniciarCategoria/{idTienda}/{categoria}")]
        public async Task<IActionResult> ReiniciarInventarioCategoria(string idTienda, string categoria)
        {
            try
            {
                // Ejecutar el procedimiento almacenado
                await _context.Database.ExecuteSqlRawAsync(
                    "EXEC ReiniciarInventarioCategoria @p0, @p1", idTienda, categoria);

                return Ok(new { mensaje = $"Inventario de categoría {categoria} reiniciado correctamente." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }*/


        [HttpPost("inventario/reiniciarCategoria/{idTienda}/{categoria}")]
        public async Task<IActionResult> ReiniciarInventarioCategoria(string idTienda, string categoria,
            [FromServices] IHubContext<NotificationHub> hubContext)
        {
            
            try
            {
                // Ejecutar el procedimiento almacenado
                await _context.Database.ExecuteSqlRawAsync(
                    "EXEC ReiniciarInventarioCategoria @p0, @p1", idTienda, categoria);

                //await hubContext.Clients.All.SendAsync("InventarioReiniciadoPorCategoria", categoria);
                // ⬇️ Enviar ambos parámetros para que el móvil filtre por tienda y sepa la categoría
                await hubContext.Clients.All.SendAsync("InventarioReiniciadoPorCategoria", idTienda, categoria);

                return Ok(new { mensaje = $"Inventario de categoría {categoria} reiniciado correctamente desde SignalR." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }


        /*****************************************************************************************/




        [HttpGet("/Inventario/detalleProducto/{idTienda}/{idProducto}")]
        public async Task<IActionResult> GetDetalleProducto(string idTienda, string idProducto)
        {
            var query = from inv in _context.InventarioTeorico
                        join tie in _context.Tiendas on inv.IdTienda equals tie.Codigo
                        join pro in _context.Productos on inv.IdProducto equals pro.Codigo
                        join inv2 in _context.InventarioFisico
                            on new { inv.IdProducto, inv.IdTienda }
                            equals new { inv2.IdProducto, inv2.IdTienda } into inv2Group
                        from inv2 in inv2Group.DefaultIfEmpty()
                        where inv.IdTienda == idTienda && inv.IdProducto == idProducto
                        select new
                        {
                            inv.IdTienda,
                            Tienda = tie.Nombre,
                            inv.IdProducto,
                            Producto = pro.Nombre,
                            pro.Categoria,
                            StockTeorico = inv.Cantidad,
                            StockFisico = inv2 != null ? inv2.CantidadLeida : 0
                        };

            var detalle = await query.FirstOrDefaultAsync();

            if (detalle == null)
                return NotFound(new { mensaje = "Producto no encontrado" });

            // Calcular faltantes, sobrantes y porcentaje
            int faltantes = detalle.StockTeorico > detalle.StockFisico
                ? detalle.StockTeorico - detalle.StockFisico
                : 0;

            int sobrantes = detalle.StockFisico > detalle.StockTeorico
                ? detalle.StockFisico - detalle.StockTeorico
                : 0;

            double progreso = detalle.StockTeorico > 0
                ? Math.Round((detalle.StockFisico * 100.0) / detalle.StockTeorico, 2)
                : 100;

            return Ok(new
            {
                detalle.IdTienda,
                detalle.Tienda,
                detalle.IdProducto,
                detalle.Producto,
                detalle.Categoria,
                detalle.StockTeorico,
                detalle.StockFisico,
                Faltantes = faltantes,
                Sobrantes = sobrantes,
                Progreso = progreso
            });
        }


        /*[HttpPost("reiniciarProducto/{idTienda}/{idProducto}")]
        public async Task<IActionResult> ReiniciarInventarioProducto(string idTienda, string idProducto)
        {
            try
            {
                // Ejecutar el procedimiento almacenado para reiniciar el inventario del producto
                await _context.Database.ExecuteSqlRawAsync(
                    "EXEC ReiniciarInventarioProducto @p0, @p1", idTienda, idProducto);
                return Ok(new { mensaje = $"Inventario del producto {idProducto} en tienda {idTienda} reiniciado correctamente." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }*/

        

        [HttpPost("reiniciarProducto/{idTienda}/{idProducto}")]
        public async Task<IActionResult> ReiniciarInventarioProducto(string idTienda, string idProducto,
            [FromServices] IHubContext<NotificationHub> hubContext)
        {
            try
            {
                // Ejecutar el procedimiento almacenado para reiniciar el inventario del producto
                await _context.Database.ExecuteSqlRawAsync(
                    "EXEC ReiniciarInventarioProducto @p0, @p1", idTienda, idProducto);

                await hubContext.Clients.All.SendAsync("InventarioReiniciadoPorProducto", idProducto);
                return Ok(new { mensaje = $"Inventario del producto {idProducto} en tienda {idTienda} reiniciado correctamente desde SignalR AFP." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        //*************************************************************************************************


        [HttpGet("tagsProducto/{idTienda}/{idProducto}")]
        public async Task<IActionResult> GetTagsPorProducto(string idTienda, string idProducto)
        {
            var tags = await _context.TagTienda
                .Where(t => t.IdTienda == idTienda && t.Ean == idProducto)
                .Select(t => new
                {
                    t.Tag,
                    t.Fecha
                })
                .OrderByDescending(t => t.Fecha)
                .ToListAsync();

            return Ok(tags);
        }
    }
}