using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AITrainingSystem.Domain.Entities;

namespace AITrainingSystem.Application.Interfaces.Repositories
{
    public interface IAssessmentResultRepository
    {
        Task<AssessmentResult> CreateAsync(AssessmentResult result);
        Task<AssessmentResult?> GetByIdAsync(Guid id);
        Task<IEnumerable<AssessmentResult>> GetByUserIdAsync(Guid userId);
        Task<AssessmentResult?> GetBestResultAsync(Guid userId, Guid quizId);
        Task SaveChangesAsync();
    }
}
