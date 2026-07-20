using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AITrainingSystem.Application.Common.Models;
using AITrainingSystem.Application.DTOs.AI;
using AITrainingSystem.Application.DTOs.Quiz;
using Microsoft.AspNetCore.Http;

namespace AITrainingSystem.Application.Interfaces.Services
{
    public interface IAIService
    {
        Task<ApiResponse<string>> AskTutorAsync(Guid userId, Guid courseId, TutorRequestDto request);
        Task<ApiResponse<QuizDto>> GenerateQuizAsync(QuizGenRequestDto request);
        Task<ApiResponse<IEnumerable<CourseRecommendationDto>>> GetRecommendationsAsync(Guid userId);
        Task<ApiResponse<MockInterviewResponseDto>> ConductMockInterviewStepAsync(Guid userId, MockInterviewStepDto request);
        Task<ApiResponse<ResumeAnalysisResultDto>> AnalyzeResumeAsync(Guid userId, ResumeAnalysisRequestDto request);

        // Advanced Stateful Multi-Modal Mock Interview Platform
        Task<ApiResponse<ExtendedMockInterviewResponseDto>> StartMockInterviewAsync(Guid userId, StartMockInterviewDto request);
        Task<ApiResponse<ExtendedMockInterviewResponseDto>> SubmitMockInterviewStepAsync(Guid userId, SubmitMockInterviewStepDto request);
        Task<ApiResponse<IEnumerable<MockInterviewSessionListItemDto>>> GetMockInterviewHistoryAsync(Guid userId);
        Task<ApiResponse<MockInterviewScorecardDto>> GetMockInterviewScorecardAsync(Guid userId, Guid id);
        Task<ApiResponse<string>> UploadInterviewVideoAsync(Guid userId, Guid id, IFormFile file);
        Task<ApiResponse<IEnumerable<AdminMockInterviewSessionDto>>> GetAllMockInterviewsAsync();
    }
}
