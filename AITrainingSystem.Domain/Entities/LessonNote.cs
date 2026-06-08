using System;

namespace AITrainingSystem.Domain.Entities
{
    public class LessonNote
    {
        public Guid Id { get; set; }

        public Guid UserId { get; set; }
        public User User { get; set; } = null!;

        public Guid LessonId { get; set; }
        public Lesson Lesson { get; set; } = null!;

        public string Content { get; set; } = string.Empty;

        public int? VideoTimestampSeconds { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}
