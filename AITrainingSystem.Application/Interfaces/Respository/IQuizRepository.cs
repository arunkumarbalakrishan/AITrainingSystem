using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AITrainingSystem.Domain.Entities;

namespace AITrainingSystem.Application.Interfaces.Repositories
{
    public interface IQuizRepository
    {
        Task<Quiz> CreateAsync(Quiz quiz);
        Task<Quiz?> GetByIdAsync(Guid id);
        Task<Quiz?> GetByIdWithQuestionsAsync(Guid id);
        Task<IEnumerable<Quiz>> GetByCourseIdAsync(Guid courseId);
        Task<Quiz?> GetFinalQuizByCourseIdAsync(Guid courseId);
        Task UpdateAsync(Quiz quiz);
        Task DeleteAsync(Quiz quiz);
        Task SaveChangesAsync();
    }
}
