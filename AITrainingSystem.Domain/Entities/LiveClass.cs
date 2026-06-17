using System;

namespace AITrainingSystem.Domain.Entities
{
    public class LiveClass
    {
        public Guid Id { get; set; }

        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        
        public string MeetingLink { get; set; } = string.Empty;
        
        public DateTime StartTime { get; set; }
        public int DurationInMinutes { get; set; }

        public Guid? CourseId { get; set; }
        public Course? Course { get; set; }

        public Guid TrainerId { get; set; }
        public User Trainer { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
