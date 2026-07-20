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

    public class StartMockInterviewDto
    {
        public string CourseTopic { get; set; } = string.Empty;
        public string Difficulty { get; set; } = "Mid"; // Junior, Mid, Senior
        public int QuestionCount { get; set; } = 5;
        public string Language { get; set; } = "English";
        public string? ResumeText { get; set; }
        public string? JobDescriptionText { get; set; }
        public string Mode { get; set; } = "Exam"; // Practice, Exam
    }

    public class SubmitMockInterviewStepDto
    {
        public string SessionId { get; set; } = string.Empty;
        public string? StudentAnswer { get; set; }
        
        // Edge analytics metrics uploaded from frontend per turn
        public double EyeContactRate { get; set; }
        public int SlouchCount { get; set; }
        public double VolumeVariance { get; set; }
        public int WordCount { get; set; }
        public List<string> FillerWords { get; set; } = new List<string>();
        public List<string> DetectedEmotions { get; set; } = new List<string>();
        public bool TabSwitched { get; set; }
        public bool ForceFinish { get; set; }
    }

    public class ExtendedMockInterviewResponseDto
    {
        public string SessionId { get; set; } = string.Empty;
        public string NextQuestionOrFeedback { get; set; } = string.Empty;
        public int? Score { get; set; }
        public string Feedback { get; set; } = string.Empty;
        public bool IsFinished { get; set; }
        
        // Hints generated for Practice Mode (ideal answers key terms)
        public List<string> Hints { get; set; } = new List<string>();
    }

    public class MockInterviewSessionListItemDto
    {
        public Guid Id { get; set; }
        public string SessionId { get; set; } = string.Empty;
        public string CourseTopic { get; set; } = string.Empty;
        public int OverallScore { get; set; }
        public bool IsCompleted { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class MockInterviewScorecardDto
    {
        public Guid Id { get; set; }
        public string SessionId { get; set; } = string.Empty;
        public string CourseTopic { get; set; } = string.Empty;
        public string ConfigSettings { get; set; } = string.Empty;
        
        public int OverallScore { get; set; }
        public int CommunicationScore { get; set; }
        public int TechnicalScore { get; set; }
        public int ConfidenceScore { get; set; }
        public int GrammarScore { get; set; }
        public int EyeContactPercentage { get; set; }
        public int BodyLanguageScore { get; set; }
        
        public string SpeechAnalyticsJson { get; set; } = "{}";
        public string BehavioralAnalyticsJson { get; set; } = "{}";
        public string QuestionByQuestionLogsJson { get; set; } = "[]";
        public string? VideoReplayUrl { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class AdminMockInterviewSessionDto
    {
        public Guid Id { get; set; }
        public string SessionId { get; set; } = string.Empty;
        public string CourseTopic { get; set; } = string.Empty;
        public string CandidateName { get; set; } = string.Empty;
        public string CandidateEmail { get; set; } = string.Empty;
        public int OverallScore { get; set; }
        public bool IsCompleted { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
