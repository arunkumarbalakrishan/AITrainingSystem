using System;

namespace AITrainingSystem.Domain.Entities
{
    public class Payment
    {
        public Guid Id { get; set; }

        public Guid UserId { get; set; }
        public User User { get; set; } = null!;

        public Guid CourseId { get; set; }
        public Course Course { get; set; } = null!;

        public decimal Amount { get; set; }
        public string Currency { get; set; } = "USD";

        // Pending, Completed, Failed
        public string Status { get; set; } = "Pending";

        public string? StripeSessionId { get; set; }
        public string? StripePaymentIntentId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}
