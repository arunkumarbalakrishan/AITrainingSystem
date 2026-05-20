using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AITrainingSystem.Application.DTOs.Lesson
{
    public class LessonResponseDto
    {
        public Guid Id { get; set; }

        public Guid CourseId { get; set; }

        public string Title { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public string VideoUrl { get; set; } = string.Empty;

        public string? PdfUrl { get; set; }

        public int DurationInMinutes { get; set; }

        public int Order { get; set; }

        public bool IsPreviewFree { get; set; }
    }
}
