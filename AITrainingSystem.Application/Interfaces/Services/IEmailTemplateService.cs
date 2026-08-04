using System.Collections.Generic;
using System.Threading.Tasks;

namespace AITrainingSystem.Application.Interfaces.Services
{
    public interface IEmailTemplateService
    {
        string GenerateHtml(string templateName, Dictionary<string, string> placeholders);
        Task QueueEmailAsync(string toEmail, string subject, string templateName, Dictionary<string, string> placeholders);
    }
}
