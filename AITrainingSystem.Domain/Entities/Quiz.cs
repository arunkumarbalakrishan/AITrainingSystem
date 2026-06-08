using System;
using System.Collections.Generic;

namespace AITrainingSystem.Domain.Entities
{
    public class Quiz
    {
        public Guid Id { get; set; }

        public Guid CourseId { get; set; }
        public Course Course { get; set; } = null!;

        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        // Passing percentage (e.g. 70 for 70%)
        public int PassingScore { get; set; }

        // If true, this is the final assessment of the course required for the certificate
        public bool IsFinal { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<Question> Questions { get; set; } = new List<Question>();
    }
}
