using System;

namespace AITrainingSystem.Domain.Entities
{
    public class EmailLog
    {
        public Guid Id { get; set; }
        public string Recipient { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string TemplateName { get; set; } = string.Empty;
        public string PlaceholdersJson { get; set; } = "{}";
        public string Status { get; set; } = "Pending";
        public int RetryCount { get; set; } = 0;
        public DateTime? SentTime { get; set; }
        public DateTime CreatedTime { get; set; } = DateTime.UtcNow;
        public string? FailureReason { get; set; }
    }
}
