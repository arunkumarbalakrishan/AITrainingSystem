using AITrainingSystem.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AITrainingSystem.Application.Interfaces.Respository
{
    public interface ILessonProgressRepository
    {
        Task<bool> ExistsAsync(Guid userId, Guid lessonId);

        Task AddAsync(LessonProgress progress);

        Task<int> GetCompletedCountAsync(Guid userId, Guid courseId);

        Task<int> GetTotalLessonsAsync(Guid courseId);
    }
}
