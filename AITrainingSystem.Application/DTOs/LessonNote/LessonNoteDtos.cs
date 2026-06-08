using System;

namespace AITrainingSystem.Application.DTOs.LessonNote
{
    public class LessonNoteDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public Guid LessonId { get; set; }
        public string Content { get; set; } = string.Empty;
        public int? VideoTimestampSeconds { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class CreateLessonNoteDto
    {
        public Guid LessonId { get; set; }
        public string Content { get; set; } = string.Empty;
        public int? VideoTimestampSeconds { get; set; }
    }

    public class UpdateLessonNoteDto
    {
        public string Content { get; set; } = string.Empty;
        public int? VideoTimestampSeconds { get; set; }
    }
}
