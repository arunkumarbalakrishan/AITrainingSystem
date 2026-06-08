using System;

namespace AITrainingSystem.Application.DTOs.Payment
{
    public class CreateCheckoutRequestDto
    {
        public Guid CourseId { get; set; }
        public string SuccessUrl { get; set; } = string.Empty;
        public string CancelUrl { get; set; } = string.Empty;
    }

    public class CheckoutSessionResponseDto
    {
        public string SessionId { get; set; } = string.Empty;
        public string CheckoutUrl { get; set; } = string.Empty;
    }
}
