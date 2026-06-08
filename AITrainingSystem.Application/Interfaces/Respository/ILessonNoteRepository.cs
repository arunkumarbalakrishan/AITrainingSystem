using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AITrainingSystem.Domain.Entities;

namespace AITrainingSystem.Application.Interfaces.Repositories
{
    public interface ILessonNoteRepository
    {
        Task<LessonNote> CreateAsync(LessonNote note);
        Task<LessonNote?> GetByIdAsync(Guid id);
        Task<IEnumerable<LessonNote>> GetByLessonIdAndUserIdAsync(Guid lessonId, Guid userId);
        Task UpdateAsync(LessonNote note);
        Task DeleteAsync(LessonNote note);
        Task SaveChangesAsync();
    }
}
