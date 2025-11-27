using Microsoft.AspNetCore.SignalR;
using RFIDInventario.Server.Services;

namespace RFIDInventario.Server.Hubs
{
    public class NotificationHub : Hub
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public NotificationHub(IServiceScopeFactory serviceScopeFactory)
        {
            _serviceScopeFactory = serviceScopeFactory;
        }

        public async Task Iniciar(string idTienda)
        {
            await Clients.All.SendAsync("Iniciar", idTienda);
        }

        /*public async Task Cerrar(string idTienda)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var inventarioService = scope.ServiceProvider.GetRequiredService<InventarioService>();
            //logica con inventarioService
            inventarioService.Cerrar(idTienda);
            await Clients.All.SendAsync("Cerrar", idTienda);
        }*/

        public async Task Reiniciar(string idTienda)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var inventarioService = scope.ServiceProvider.GetRequiredService<InventarioService>();
            //logica con inventarioService
            inventarioService.Reiniciar(idTienda);
            await Clients.All.SendAsync("Reiniciar", idTienda);
        }


        //**********************************************************************
        /*public async Task EnviarActualizacion(string idTienda, object data)
        {
            // Envía los datos de actualización solo al grupo correspondiente a la tienda
            await Clients.Group(idTienda).SendAsync("ActualizarDatos", data);
        }*/

        /* public override async Task OnConnectedAsync()
         {
             var httpContext = Context.GetHttpContext();
             var idTienda = httpContext?.Request.Query["idTienda"];

             if (!string.IsNullOrEmpty(idTienda))
             {
                 await Groups.AddToGroupAsync(Context.ConnectionId, idTienda);
             }

             await base.OnConnectedAsync();
         }

         public override async Task OnDisconnectedAsync(Exception? exception)
         {
             var httpContext = Context.GetHttpContext();
             var idTienda = httpContext?.Request.Query["idTienda"];

             if (!string.IsNullOrEmpty(idTienda))
             {
                 await Groups.RemoveFromGroupAsync(Context.ConnectionId, idTienda);
             }

             await base.OnDisconnectedAsync(exception);
         }

         //***********************************************************************/
        //conexion grupo tienda
        public override async Task OnConnectedAsync()
        {
            var idTienda = Context.GetHttpContext()?.Request.Query["idTienda"];

            if (!string.IsNullOrEmpty(idTienda))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, idTienda);
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var idTienda = Context.GetHttpContext()?.Request.Query["idTienda"];

            if (!string.IsNullOrEmpty(idTienda))
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, idTienda);
            }

            await base.OnDisconnectedAsync(exception);
        }



        //*****************************************USUARIOS**************************************************/
        public async Task UsuarioCreado(object usuario)
        {
            await Clients.All.SendAsync("UsuarioCreado", usuario);
        }

        public async Task UsuarioActualizado(object usuario)
        {
            await Clients.All.SendAsync("UsuarioActualizado", usuario);
        }

        public async Task UsuarioEliminado(int id)
        {
            await Clients.All.SendAsync("UsuarioEliminado", id);
        }

        //*****************************************ASIGNACIONES USUARIO-TIENDA**************************************************//
        public async Task TiendaAsignada(object asignacion)
        {
            await Clients.All.SendAsync("TiendaAsignada", asignacion);
        }

        public async Task TiendaDesasignada(int id)
        {
            await Clients.All.SendAsync("TiendaDesasignada", id);
        }

        //estado
        public async Task EstadoTiendaActualizado(string idTienda, string estado)
        {
            await Clients.All.SendAsync("EstadoTiendaActualizado", new { idTienda, estado });
        }

    }
}
