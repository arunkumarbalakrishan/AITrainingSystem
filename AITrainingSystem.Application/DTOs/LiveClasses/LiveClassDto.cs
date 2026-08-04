using System;

namespace AITrainingSystem.Application.DTOs.LiveClasses
{
    public class CreateLiveClassDto
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? MeetingLink { get; set; }
        public string Provider { get; set; } = "Auto"; // "Auto" / "Jitsi" / "Zoom" / "Custom"
        public DateTime StartTime { get; set; }
        public int DurationInMinutes { get; set; }
        public Guid? CourseId { get; set; }
    }

    public class LiveClassDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string MeetingLink { get; set; } = string.Empty;
        public string Provider { get; set; } = "Jitsi"; // "Jitsi" / "Zoom" / "Custom"
        public string? Passcode { get; set; }
        public string? HostStartUrl { get; set; }
        public DateTime StartTime { get; set; }
        public int DurationInMinutes { get; set; }
        public Guid? CourseId { get; set; }
        public string? CourseTitle { get; set; }
        public Guid TrainerId { get; set; }
        public string TrainerName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class ZoomMeetingResponseDto
    {
        public bool Success { get; set; }
        public string MeetingLink { get; set; } = string.Empty;
        public string HostStartUrl { get; set; } = string.Empty;
        public string Passcode { get; set; } = string.Empty;
        public string MeetingId { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
    }
}
