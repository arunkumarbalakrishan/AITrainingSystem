using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace AITrainingSystem.API.Hubs
{
    [Authorize]
    public class NotificationHub : Hub
    {
        // Clients connect to this hub using their JWT.
        // We don't need any explicit methods here because the server 
        // will push messages directly to clients based on their UserId claim.
    }
}
