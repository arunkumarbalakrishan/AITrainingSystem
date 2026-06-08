using System;
using System.Collections.Generic;

namespace AITrainingSystem.Domain.Entities
{
    public class Question
    {
        public Guid Id { get; set; }

        public Guid QuizId { get; set; }
        public Quiz Quiz { get; set; } = null!;

        public string Text { get; set; } = string.Empty;

        public int Points { get; set; } = 1;

        // "SingleChoice", "MultipleChoice", etc.
        public string QuestionType { get; set; } = "SingleChoice";

        public ICollection<QuestionOption> Options { get; set; } = new List<QuestionOption>();
    }
}
