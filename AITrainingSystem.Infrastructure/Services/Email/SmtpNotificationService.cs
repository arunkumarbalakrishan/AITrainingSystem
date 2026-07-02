using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Mail;
using System.Text;
using System.Text.Json;
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
            var resendApiKey = _configuration["Resend:ApiKey"];
            if (!string.IsNullOrEmpty(resendApiKey))
            {
                _logger.LogInformation("Attempting to send email to {ToEmail} using Resend HTTPS API", toEmail);
                try
                {
                    using var httpClient = new HttpClient();
                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", resendApiKey);

                    var fromAddress = _configuration["Resend:FromAddress"] ?? "onboarding@resend.dev";

                    var payload = new
                    {
                        from = fromAddress,
                        to = new[] { toEmail },
                        subject = subject,
                        html = body
                    };

                    var json = JsonSerializer.Serialize(payload);
                    using var content = new StringContent(json, Encoding.UTF8, "application/json");

                    var response = await httpClient.PostAsync("https://api.resend.com/emails", content);
                    if (!response.IsSuccessStatusCode)
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        throw new Exception($"Resend API returned status code {response.StatusCode}: {errorContent}");
                    }

                    _logger.LogInformation("Email sent successfully via Resend API to {ToEmail}", toEmail);
                    return;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send email via Resend API to {ToEmail}.", toEmail);
                    throw;
                }
            }

            var brevoApiKey = _configuration["Brevo:ApiKey"];
            if (!string.IsNullOrEmpty(brevoApiKey))
            {
                _logger.LogInformation("Attempting to send email to {ToEmail} using Brevo HTTPS API", toEmail);
                try
                {
                    using var httpClient = new HttpClient();
                    httpClient.DefaultRequestHeaders.Add("api-key", brevoApiKey);
                    httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    var fromAddress = _configuration["Brevo:FromAddress"] ?? "no-reply@aitrainingsystem.com";
                    var fromName = _configuration["Brevo:FromName"] ?? "AI Training System";

                    var payload = new
                    {
                        sender = new
                        {
                            name = fromName,
                            email = fromAddress
                        },
                        to = new[]
                        {
                            new { email = toEmail }
                        },
                        subject = subject,
                        htmlContent = body
                    };

                    var json = JsonSerializer.Serialize(payload);
                    using var content = new StringContent(json, Encoding.UTF8, "application/json");

                    var response = await httpClient.PostAsync("https://api.brevo.com/v3/smtp/email", content);
                    if (!response.IsSuccessStatusCode)
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        throw new Exception($"Brevo API returned status code {response.StatusCode}: {errorContent}");
                    }

                    _logger.LogInformation("Email sent successfully via Brevo API to {ToEmail}", toEmail);
                    return;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send email via Brevo API to {ToEmail}.", toEmail);
                    throw;
                }
            }

            var host = _configuration["Smtp:Host"];
            var portStr = _configuration["Smtp:Port"];
            var username = _configuration["Smtp:Username"];
            var password = _configuration["Smtp:Password"];
            var fromAddressSmtp = _configuration["Smtp:FromAddress"] ?? "no-reply@aitrainingsystem.com";

            _logger.LogInformation("Attempting to send email to {ToEmail} with subject '{Subject}' using SMTP", toEmail, subject);

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
                    From = new MailAddress(fromAddressSmtp),
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
                _logger.LogError(ex, "Failed to send email to {ToEmail}.", toEmail);
                throw;
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
