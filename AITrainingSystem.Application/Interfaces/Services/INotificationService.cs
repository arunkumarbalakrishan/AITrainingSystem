using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AITrainingSystem.Application.Common.Models;
using AITrainingSystem.Application.DTOs.Notification;

namespace AITrainingSystem.Application.Interfaces.Services
{
    public interface INotificationService
    {
        Task SendEmailAsync(string toEmail, string subject, string body);
        Task<NotificationDto> CreateInAppNotificationAsync(Guid userId, string title, string message);
        Task<ApiResponse<IEnumerable<NotificationDto>>> GetNotificationsForUserAsync(Guid userId);
        Task<ApiResponse<bool>> MarkAsReadAsync(Guid notificationId);
    }
}
