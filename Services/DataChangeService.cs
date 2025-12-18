using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using RFIDInventario.Server.Data;
using RFIDInventario.Server.Hubs;



namespace RFIDInventario.Server.Services
{
    public class DataChangeService(IServiceScopeFactory serviceScopeFactory,
        IHubContext<NotificationHub> hubContext) : BackgroundService
    {
        private readonly IServiceScopeFactory _serviceScopeFactory = serviceScopeFactory;
        private readonly IHubContext<NotificationHub> _hubContext = hubContext;
        private int productos_;
        private int productosCantidad_;
        private int productosLectura_;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while ((!stoppingToken.IsCancellationRequested))
            {
                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    var query = from inv in dbContext.InventarioTeorico
                                join tie in dbContext.Tiendas on inv.IdTienda equals tie.Codigo
                                join pro in dbContext.Productos on inv.IdProducto equals pro.Codigo
                                join inv2 in dbContext.InventarioFisico on new { inv.IdProducto, inv.IdTienda }
                                equals new { inv2.IdProducto, inv2.IdTienda } into inv2Group
                                from inv2 in inv2Group.DefaultIfEmpty()

                                select new
                                {
                                    inv.IdTienda,
                                    Tienda = tie.Nombre,
                                    inv.IdProducto,
                                    Producto = pro.Nombre,
                                    pro.Categoria,
                                    inv.Cantidad,
                                    CantidadLectura = inv2 != null ? inv2.CantidadLeida : 0          //opoerador ternario pra manejo de nulos
                                };

                    var productosConInventario = await query.ToListAsync(stoppingToken);
                    var productos = productosConInventario.Count;
                    var productosCantidad = productosConInventario.Sum(x => x.Cantidad);
                    var productosLectura = productosConInventario.Sum(x => x.CantidadLectura);
                    if (productos != productos_ || productosCantidad != productosCantidad_ || 
                        productosLectura != productosLectura_)
                    {
                        productos_= productos;
                        productosCantidad_ = productosCantidad;
                        productosLectura_ = productosLectura;
                        //await _hubContext.Clients.All.SendAsync("InventarioActualizado", stoppingToken);
                        await _hubContext.Clients.All.SendAsync("InventarioActualizado"); 
                        Console.WriteLine("**************Evento InventarioActualizado enviado para prueba automatica");

                        /*foreach (var tienda in productosConInventario.Select(p => p.IdTienda).Distinct())
                        {
                            await _hubContext.Clients.Group(tienda).SendAsync("ActualizarInventario", tienda, stoppingToken);
                        }*/
                    }
                }
                
                await Task.Delay(3000, stoppingToken); // 5 segundos antes de la siguiente verificación
            }
        }
    }
}
