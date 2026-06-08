using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AITrainingSystem.Application.Common.Models;
using AITrainingSystem.Application.DTOs.Quiz;

namespace AITrainingSystem.Application.Interfaces.Services
{
    public interface IAssessmentService
    {
        Task<ApiResponse<QuizDto>> CreateQuizAsync(CreateQuizDto dto);
        Task<ApiResponse<QuizDto>> GetQuizByIdAsync(Guid quizId);
        Task<ApiResponse<IEnumerable<QuizDto>>> GetQuizzesByCourseIdAsync(Guid courseId);
        Task<ApiResponse<AssessmentResultDto>> SubmitQuizAsync(Guid userId, QuizSubmitDto submission);
        Task<ApiResponse<IEnumerable<AssessmentResultDto>>> GetUserResultsAsync(Guid userId);
        Task<bool> HasUserPassedFinalAssessmentAsync(Guid userId, Guid courseId);
    }
}
