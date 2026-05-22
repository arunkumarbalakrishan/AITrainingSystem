using AITrainingSystem.Application.DTOs.Progress;

public interface ILessonProgressService
{
    Task CompleteLessonAsync(Guid userId, Guid lessonId);

    Task<CourseProgressDto> GetCourseProgressAsync(
        Guid userId,
        Guid courseId);
  
}