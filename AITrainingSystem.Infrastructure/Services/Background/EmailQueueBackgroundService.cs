using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AITrainingSystem.Application.Interfaces.Repositories;
using AITrainingSystem.Application.Interfaces.Services;
using AITrainingSystem.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AITrainingSystem.Infrastructure.Services.Background
{
    public class EmailQueueBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<EmailQueueBackgroundService> _logger;
        private const int MaxRetries = 3;
        private const int DelaySeconds = 10;

        public EmailQueueBackgroundService(
            IServiceProvider serviceProvider,
            ILogger<EmailQueueBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Email Queue Background Service starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessEmailQueueAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred while processing core email queue loop.");
                }

                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(DelaySeconds), stoppingToken);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
            }

            _logger.LogInformation("Email Queue Background Service is stopping.");
        }

        private async Task ProcessEmailQueueAsync(CancellationToken stoppingToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var emailLogRepo = scope.ServiceProvider.GetRequiredService<IEmailLogRepository>();
            var templateService = scope.ServiceProvider.GetRequiredService<IEmailTemplateService>();
            var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

            var pendingLogs = await emailLogRepo.GetPendingOrFailedEmailsAsync(MaxRetries);

            foreach (var log in pendingLogs)
            {
                if (stoppingToken.IsCancellationRequested)
                    break;

                _logger.LogInformation("Processing queued email LogId {LogId} for {Recipient}", log.Id, log.Recipient);

                try
                {
                    // 1. Deserialize placeholders
                    Dictionary<string, string> placeholders;
                    try
                    {
                        placeholders = JsonSerializer.Deserialize<Dictionary<string, string>>(log.PlaceholdersJson) 
                                       ?? new Dictionary<string, string>();
                    }
                    catch (Exception jsonEx)
                    {
                        _logger.LogError(jsonEx, "Failed to deserialize placeholders for LogId {LogId}. Using empty dictionary.", log.Id);
                        placeholders = new Dictionary<string, string>();
                    }

                    // 2. Generate responsive HTML using Master/Detail assembly
                    string compiledHtml = templateService.GenerateHtml(log.TemplateName, placeholders);

                    // 3. Send email via SMTP/Brevo/Resend Dispatcher
                    await notificationService.SendEmailAsync(log.Recipient, log.Subject, compiledHtml);

                    // 4. Update state on success
                    log.Status = "Sent";
                    log.SentTime = DateTime.UtcNow;
                    log.FailureReason = null;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send queued email LogId {LogId} (attempt {Attempt})", log.Id, log.RetryCount + 1);

                    log.RetryCount++;
                    log.Status = log.RetryCount >= MaxRetries ? "FailedPermanent" : "Failed";
                    log.FailureReason = ex.Message;
                }

                // Update database
                await emailLogRepo.UpdateAsync(log);
                await emailLogRepo.SaveChangesAsync();
            }
        }
    }
}
