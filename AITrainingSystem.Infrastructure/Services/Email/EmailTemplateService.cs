using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AITrainingSystem.Application.Interfaces.Repositories;
using AITrainingSystem.Application.Interfaces.Services;
using AITrainingSystem.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace AITrainingSystem.Infrastructure.Services.Email
{
    public class EmailTemplateService : IEmailTemplateService
    {
        private readonly IEmailLogRepository _emailLogRepository;
        private readonly ILogger<EmailTemplateService> _logger;

        public EmailTemplateService(
            IEmailLogRepository emailLogRepository,
            ILogger<EmailTemplateService> logger)
        {
            _emailLogRepository = emailLogRepository;
            _logger = logger;
        }

        public async Task QueueEmailAsync(string toEmail, string subject, string templateName, Dictionary<string, string> placeholders)
        {
            try
            {
                var emailLog = new EmailLog
                {
                    Id = Guid.NewGuid(),
                    Recipient = toEmail,
                    Subject = subject,
                    TemplateName = templateName,
                    PlaceholdersJson = JsonSerializer.Serialize(placeholders ?? new Dictionary<string, string>()),
                    Status = "Pending",
                    RetryCount = 0,
                    CreatedTime = DateTime.UtcNow
                };

                await _emailLogRepository.AddAsync(emailLog);
                await _emailLogRepository.SaveChangesAsync();
                _logger.LogInformation("Email queued for {Recipient} using template {TemplateName}", toEmail, templateName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to queue email for {Recipient}", toEmail);
                throw;
            }
        }

        public string GenerateHtml(string templateName, Dictionary<string, string> placeholders)
        {
            var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            var baseTemplatePath = Path.Combine(baseDirectory, "Templates", "BaseTemplate.html");
            var childTemplatePath = Path.Combine(baseDirectory, "Templates", $"{templateName}.html");

            // 1. Load Base Template
            string baseHtml;
            if (File.Exists(baseTemplatePath))
            {
                baseHtml = File.ReadAllText(baseTemplatePath);
            }
            else
            {
                _logger.LogWarning("BaseTemplate.html not found at path '{Path}'. Using embedded C# fallback.", baseTemplatePath);
                baseHtml = GetEmbeddedBaseTemplateFallback();
            }

            // 2. Load Child Template Body Content
            string childBodyHtml = string.Empty;
            if (File.Exists(childTemplatePath))
            {
                childBodyHtml = File.ReadAllText(childTemplatePath);
            }
            else
            {
                _logger.LogError("Child template '{TemplateName}' not found at path '{Path}'. Using blank layout body.", templateName, childTemplatePath);
                // Return fallback for base template replacement
                childBodyHtml = "<div><p>Email content unavailable.</p></div>";
            }

            // 3. Assemble components by placing Child HTML into Base Template
            string assembledHtml = baseHtml.Replace("{{EmailBody}}", childBodyHtml);

            // 4. Set current year automatically if not provided
            if (!placeholders.ContainsKey("CurrentYear"))
            {
                placeholders["CurrentYear"] = DateTime.UtcNow.Year.ToString();
            }

            // 5. Replace placeholders
            foreach (var kp in placeholders)
            {
                var placeholderKey = "{{" + kp.Key + "}}";
                assembledHtml = assembledHtml.Replace(placeholderKey, kp.Value ?? string.Empty);
            }

            // 6. Clean up any remaining unreplaced placeholders to ensure visual polish
            assembledHtml = Regex.Replace(assembledHtml, @"\{\{\w+\}\}", string.Empty);

            return assembledHtml;
        }

        private string GetEmbeddedBaseTemplateFallback()
        {
            return @"<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>AI Online Training System</title>
</head>
<body style='margin:0; padding:0; background-color:#F5F7FA; font-family:-apple-system,BlinkMacSystemFont,charcoal,sans-serif;'>
    <table width='100%' cellpadding='0' cellspacing='0' border='0' bgcolor='#F5F7FA' style='padding: 20px 0;'>
        <tr>
            <td align='center'>
                <table width='100%' cellpadding='0' cellspacing='0' border='0' style='max-width:600px; background-color:#FFFFFF; border-radius:12px; overflow:hidden; box-shadow:0 4px 6px rgba(0,0,0,0.05);'>
                    <tr>
                        <td align='center' bgcolor='#111827' style='padding:25px;'>
                            <h2 style='color:#ffffff; margin:0;'>AI Online Training System</h2>
                            <p style='color:#9FEF00; font-size:12px; margin:5px 0 0 0; text-transform:uppercase; letter-spacing:1px;'>Learn • Practice • Get Certified</p>
                        </td>
                    </tr>
                    <tr>
                        <td style='padding:30px; color:#2C2C2C; font-size:16px; line-height:1.6;'>
                            {{EmailBody}}
                        </td>
                    </tr>
                    <tr>
                        <td align='center' bgcolor='#F9FAFB' style='padding:20px; border-top:1px solid #E5E7EB; font-size:12px; color:#9CA3AF;'>
                            <p style='margin:0 0 5px 0;'>&copy; {{CurrentYear}} AI Online Training System. All rights reserved.</p>
                            <p style='margin:0;'>Learn &bull; Practice &bull; Get Certified</p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>";
        }
    }
}
