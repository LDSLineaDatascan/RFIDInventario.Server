using Microsoft.AspNetCore.SignalR;
using RFIDInventario.Server.Hubs;

namespace RFIDInventario.Server.Services
{
    public class NotificationService(IHubContext<NotificationHub> hubContext)
    {
        private readonly IHubContext<NotificationHub> _hubContext = hubContext;
        
        public async Task CheckForUpdates()
        {
            await _hubContext.Clients.All.SendAsync("RecieveMessage", "Datos actualizados");
        }
    }
}
