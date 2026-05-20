using AITrainingSystem.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AITrainingSystem.Application.Interfaces.Lessons
{
    public interface ILessonRepository
    {
        Task<Lesson?> GetByIdAsync(Guid id);
        Task<List<Lesson>> GetByCourseIdAsync(Guid courseId);
        Task<int> GetNextOrderAsync(Guid courseId);
        Task<bool> OrderExistsAsync(Guid courseId, int order);
        Task AddAsync(Lesson lesson);
        Task UpdateAsync(Lesson lesson);
        Task DeleteAsync(Lesson lesson);
    }
}
