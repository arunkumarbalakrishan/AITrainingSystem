using System;
using System.Threading.Tasks;
using AITrainingSystem.Application.DTOs.Notification;

namespace AITrainingSystem.Application.Interfaces.Services
{
    public interface IRealTimeNotificationService
    {
        Task SendToUserAsync(Guid userId, NotificationDto notification);
    }
}
