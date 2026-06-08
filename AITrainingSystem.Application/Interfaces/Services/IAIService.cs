using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AITrainingSystem.Application.Common.Models;
using AITrainingSystem.Application.DTOs.AI;
using AITrainingSystem.Application.DTOs.Quiz;

namespace AITrainingSystem.Application.Interfaces.Services
{
    public interface IAIService
    {
        Task<ApiResponse<string>> AskTutorAsync(Guid userId, Guid courseId, TutorRequestDto request);
        Task<ApiResponse<QuizDto>> GenerateQuizAsync(QuizGenRequestDto request);
        Task<ApiResponse<IEnumerable<CourseRecommendationDto>>> GetRecommendationsAsync(Guid userId);
        Task<ApiResponse<MockInterviewResponseDto>> ConductMockInterviewStepAsync(Guid userId, MockInterviewStepDto request);
        Task<ApiResponse<ResumeAnalysisResultDto>> AnalyzeResumeAsync(Guid userId, ResumeAnalysisRequestDto request);
    }
}
