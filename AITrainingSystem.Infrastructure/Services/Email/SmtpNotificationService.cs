using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using AITrainingSystem.Application.Common.Models;
using AITrainingSystem.Application.DTOs.Notification;
using AITrainingSystem.Application.Interfaces.Repositories;
using AITrainingSystem.Application.Interfaces.Services;
using AITrainingSystem.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AITrainingSystem.Infrastructure.Services.Email
{
    public class SmtpNotificationService : INotificationService
    {
        private readonly INotificationRepository _repo;
        private readonly IConfiguration _configuration;
        private readonly ILogger<SmtpNotificationService> _logger;
        private readonly IRealTimeNotificationService _realTimeNotificationService;

        public SmtpNotificationService(
            INotificationRepository repo, 
            IConfiguration configuration, 
            ILogger<SmtpNotificationService> logger,
            IRealTimeNotificationService realTimeNotificationService)
        {
            _repo = repo;
            _configuration = configuration;
            _logger = logger;
            _realTimeNotificationService = realTimeNotificationService;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            var host = _configuration["Smtp:Host"];
            var portStr = _configuration["Smtp:Port"];
            var username = _configuration["Smtp:Username"];
            var password = _configuration["Smtp:Password"];
            var fromAddress = _configuration["Smtp:FromAddress"] ?? "no-reply@aitrainingsystem.com";

            _logger.LogInformation("Attempting to send email to {ToEmail} with subject '{Subject}'", toEmail, subject);

            if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(username))
            {
                _logger.LogWarning("SMTP Configuration missing. Fallback: Logging email message body:\n{Body}", body);
                return;
            }

            try
            {
                int port = int.TryParse(portStr, out var p) ? p : 587;
                using var mailMessage = new MailMessage
                {
                    From = new MailAddress(fromAddress),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                };

                mailMessage.To.Add(toEmail);

                using var smtpClient = new SmtpClient(host, port)
                {
                    Credentials = new NetworkCredential(username, password),
                    EnableSsl = true
                };

                await smtpClient.SendMailAsync(mailMessage);
                _logger.LogInformation("Email sent successfully to {ToEmail}", toEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {ToEmail}. Falling back to logs.", toEmail);
            }
        }

        public async Task<NotificationDto> CreateInAppNotificationAsync(Guid userId, string title, string message)
        {
            var notification = new Notification
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Title = title,
                Message = message,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            await _repo.CreateAsync(notification);
            await _repo.SaveChangesAsync();

            var dto = new NotificationDto
            {
                Id = notification.Id,
                UserId = notification.UserId,
                Title = notification.Title,
                Message = notification.Message,
                IsRead = notification.IsRead,
                CreatedAt = notification.CreatedAt
            };

            try 
            {
                await _realTimeNotificationService.SendToUserAsync(userId, dto);
            } 
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to broadcast real-time notification to user {UserId}", userId);
            }

            return dto;
        }

        public async Task<ApiResponse<IEnumerable<NotificationDto>>> GetNotificationsForUserAsync(Guid userId)
        {
            var notifications = await _repo.GetByUserIdAsync(userId);
            var result = notifications.Select(n => new NotificationDto
            {
                Id = n.Id,
                UserId = n.UserId,
                Title = n.Title,
                Message = n.Message,
                IsRead = n.IsRead,
                CreatedAt = n.CreatedAt
            });

            return ApiResponse<IEnumerable<NotificationDto>>.SuccessResponse(result, "Notifications retrieved successfully.");
        }

        public async Task<ApiResponse<bool>> MarkAsReadAsync(Guid notificationId)
        {
            var notification = await _repo.GetByIdAsync(notificationId);
            if (notification == null)
            {
                return ApiResponse<bool>.FailResponse("Notification not found.");
            }

            notification.IsRead = true;
            await _repo.UpdateAsync(notification);
            await _repo.SaveChangesAsync();

            return ApiResponse<bool>.SuccessResponse(true, "Notification marked as read.");
        }
    }
}
