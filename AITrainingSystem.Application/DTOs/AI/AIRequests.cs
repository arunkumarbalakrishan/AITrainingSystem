using System;
using System.Collections.Generic;

namespace AITrainingSystem.Application.DTOs.AI
{
    public class TutorRequestDto
    {
        public Guid LessonId { get; set; }
        public string Question { get; set; } = string.Empty;
    }

    public class QuizGenRequestDto
    {
        public Guid CourseId { get; set; }
        public string CourseTopic { get; set; } = string.Empty;
        public int QuestionCount { get; set; } = 5;
    }

    public class CourseRecommendationDto
    {
        public Guid CourseId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
    }

    public class MockInterviewStepDto
    {
        public string CourseTopic { get; set; } = string.Empty;
        public string? StudentAnswer { get; set; }
        public List<ChatMessageDto> ChatHistory { get; set; } = new List<ChatMessageDto>();
    }

    public class ChatMessageDto
    {
        public string Role { get; set; } = "user"; // "user", "assistant", "system"
        public string Content { get; set; } = string.Empty;
    }

    public class MockInterviewResponseDto
    {
        public string NextQuestionOrFeedback { get; set; } = string.Empty;
        public int? Score { get; set; } // Provided at the end of the session
        public string Feedback { get; set; } = string.Empty;
        public bool IsFinished { get; set; }
    }

    public class ResumeAnalysisRequestDto
    {
        public string ResumeText { get; set; } = string.Empty;
    }

    public class ResumeAnalysisResultDto
    {
        public int MatchScore { get; set; } // Match score against taken courses
        public string Critique { get; set; } = string.Empty;
        public List<string> SkillsStrengths { get; set; } = new List<string>();
        public List<string> SuggestedCourses { get; set; } = new List<string>();
    }
}
