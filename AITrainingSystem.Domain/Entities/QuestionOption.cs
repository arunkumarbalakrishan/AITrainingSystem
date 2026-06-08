using System;

namespace AITrainingSystem.Domain.Entities
{
    public class QuestionOption
    {
        public Guid Id { get; set; }

        public Guid QuestionId { get; set; }
        public Question Question { get; set; } = null!;

        public string OptionText { get; set; } = string.Empty;

        public bool IsCorrect { get; set; }
    }
}
