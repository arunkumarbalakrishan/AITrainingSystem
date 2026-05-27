using AITrainingSystem.Application.DTOs.Lesson;
using AITrainingSystem.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AITrainingSystem.Application.DTOs.Progress
{
    public class ContinueWatchingDto
    {
        public Guid CourseId { get; set; }

        public string CourseTitle { get; set; } = default!;

        public Guid LessonId { get; set; }

        public string LessonTitle { get; set; } = default!;

        public int LastWatchedSecond { get; set; }

        public decimal WatchPercentage { get; set; }

        public DateTime UpdatedAt { get; set; }

    }
}
