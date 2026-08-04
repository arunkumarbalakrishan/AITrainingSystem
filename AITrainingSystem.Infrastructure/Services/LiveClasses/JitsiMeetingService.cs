using System;
using System.Text.RegularExpressions;
using AITrainingSystem.Application.Interfaces.Services;

namespace AITrainingSystem.Infrastructure.Services.LiveClasses
{
    public class JitsiMeetingService : IJitsiMeetingService
    {
        public string GenerateMeetingLink(string topic, Guid classId)
        {
            var sanitizedTopic = Regex.Replace(topic ?? "LiveClass", @"[^a-zA-Z0-9]", "");
            if (string.IsNullOrWhiteSpace(sanitizedTopic))
            {
                sanitizedTopic = "LiveSession";
            }
            var shortId = classId.ToString("N").Substring(0, 8);
            var roomName = $"AITrainingSystem-{sanitizedTopic}-{shortId}";
            return $"https://meet.jit.si/{roomName}";
        }
    }
}
