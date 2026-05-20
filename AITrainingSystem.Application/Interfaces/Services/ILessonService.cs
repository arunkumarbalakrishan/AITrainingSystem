using AITrainingSystem.Application.DTOs.Lesson;

public interface ILessonService
{
    Task<Guid> CreateAsync(CreateLessonDto dto);
    Task<List<LessonResponseDto>> GetByCourseIdAsync(Guid courseId);
    Task<LessonResponseDto> GetByIdAsync(Guid id);
    Task<bool> UpdateAsync(Guid id, UpdateLessonDto dto);
    Task<bool> DeleteAsync(Guid id);
}