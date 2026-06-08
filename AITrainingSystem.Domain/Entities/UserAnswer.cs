using System;

namespace AITrainingSystem.Domain.Entities
{
    public class UserAnswer
    {
        public Guid Id { get; set; }

        public Guid AssessmentResultId { get; set; }
        public AssessmentResult AssessmentResult { get; set; } = null!;

        public Guid QuestionId { get; set; }
        public Question Question { get; set; } = null!;

        public Guid SelectedOptionId { get; set; }
        public QuestionOption SelectedOption { get; set; } = null!;
    }
}
