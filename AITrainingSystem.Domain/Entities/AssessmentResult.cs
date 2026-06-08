using System;
using System.Collections.Generic;

namespace AITrainingSystem.Domain.Entities
{
    public class AssessmentResult
    {
        public Guid Id { get; set; }

        public Guid UserId { get; set; }
        public User User { get; set; } = null!;

        public Guid QuizId { get; set; }
        public Quiz Quiz { get; set; } = null!;

        public int Score { get; set; } // percentage score (e.g. 80)

        public bool IsPassed { get; set; }

        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

        public ICollection<UserAnswer> Answers { get; set; } = new List<UserAnswer>();
    }
}
