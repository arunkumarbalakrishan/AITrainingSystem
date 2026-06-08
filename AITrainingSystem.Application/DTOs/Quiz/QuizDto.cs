using System;
using System.Collections.Generic;

namespace AITrainingSystem.Application.DTOs.Quiz
{
    public class QuizDto
    {
        public Guid Id { get; set; }
        public Guid CourseId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int PassingScore { get; set; }
        public bool IsFinal { get; set; }
        public List<QuestionDto> Questions { get; set; } = new List<QuestionDto>();
    }

    public class QuestionDto
    {
        public Guid Id { get; set; }
        public string Text { get; set; } = string.Empty;
        public int Points { get; set; }
        public string QuestionType { get; set; } = string.Empty;
        public List<OptionDto> Options { get; set; } = new List<OptionDto>();
    }

    public class OptionDto
    {
        public Guid Id { get; set; }
        public string OptionText { get; set; } = string.Empty;
        public bool? IsCorrect { get; set; } // Nullable so correct status is hidden from students
    }

    public class CreateQuizDto
    {
        public Guid CourseId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int PassingScore { get; set; } = 70;
        public bool IsFinal { get; set; }
        public List<CreateQuestionDto> Questions { get; set; } = new List<CreateQuestionDto>();
    }

    public class CreateQuestionDto
    {
        public string Text { get; set; } = string.Empty;
        public int Points { get; set; } = 1;
        public string QuestionType { get; set; } = "SingleChoice";
        public List<CreateOptionDto> Options { get; set; } = new List<CreateOptionDto>();
    }

    public class CreateOptionDto
    {
        public string OptionText { get; set; } = string.Empty;
        public bool IsCorrect { get; set; }
    }
}
