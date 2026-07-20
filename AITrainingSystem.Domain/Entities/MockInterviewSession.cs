using System;

namespace AITrainingSystem.Domain.Entities
{
    public class MockInterviewSession
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public User User { get; set; } = null!;
        public string SessionId { get; set; } = string.Empty;
        public string CourseTopic { get; set; } = string.Empty;
        
        // JSON configurations (difficulty, questions count, mode, resume_context, job_description)
        public string ConfigSettings { get; set; } = "{}";
        
        // Performance metrics
        public int OverallScore { get; set; }
        public int CommunicationScore { get; set; }
        public int TechnicalScore { get; set; }
        public int ConfidenceScore { get; set; }
        public int GrammarScore { get; set; }
        public int EyeContactPercentage { get; set; }
        public int BodyLanguageScore { get; set; }
        
        // Detailed extraction logs (JSON strings)
        public string SpeechAnalyticsJson { get; set; } = "{}";
        public string BehavioralAnalyticsJson { get; set; } = "{}";
        public string QuestionByQuestionLogsJson { get; set; } = "[]";
        public string? VideoReplayUrl { get; set; }
        
        public bool IsCompleted { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
