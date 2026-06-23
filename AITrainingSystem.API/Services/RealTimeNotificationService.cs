using System;
using System.Threading.Tasks;
using AITrainingSystem.API.Hubs;
using AITrainingSystem.Application.DTOs.Notification;
using AITrainingSystem.Application.Interfaces.Services;
using Microsoft.AspNetCore.SignalR;

namespace AITrainingSystem.API.Services
{
    public class RealTimeNotificationService : IRealTimeNotificationService
    {
        private readonly IHubContext<NotificationHub> _hubContext;

        public RealTimeNotificationService(IHubContext<NotificationHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public async Task SendToUserAsync(Guid userId, NotificationDto notification)
        {
            // Send to a specific user (requires UserIdProvider mapping in SignalR, usually matches ClaimTypes.NameIdentifier)
            await _hubContext.Clients.User(userId.ToString()).SendAsync("ReceiveNotification", notification);
        }
    }
}
