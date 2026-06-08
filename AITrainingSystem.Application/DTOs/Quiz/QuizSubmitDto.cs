using System;
using System.Collections.Generic;

namespace AITrainingSystem.Application.DTOs.Quiz
{
    public class QuizSubmitDto
    {
        public Guid QuizId { get; set; }
        public List<AnswerSubmitDto> Answers { get; set; } = new List<AnswerSubmitDto>();
    }

    public class AnswerSubmitDto
    {
        public Guid QuestionId { get; set; }
        public Guid SelectedOptionId { get; set; }
    }

    public class AssessmentResultDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public Guid QuizId { get; set; }
        public int Score { get; set; }
        public bool IsPassed { get; set; }
        public DateTime SubmittedAt { get; set; }
    }
}
