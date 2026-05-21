using AITrainingSystem.Application.DTOs.Lesson;

public interface ILessonService
{
    Task<Guid> CreateAsync(CreateLessonDto dto);
    Task<List<LessonResponseDto>> GetByCourseIdAsync(Guid courseId);
    Task<LessonAccessDto> GetByIdAsync(Guid lessonId, Guid userId);
    Task<bool> UpdateAsync(Guid id, UpdateLessonDto dto);
    Task<bool> DeleteAsync(Guid id);
}