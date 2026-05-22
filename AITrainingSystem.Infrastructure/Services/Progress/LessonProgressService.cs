using AITrainingSystem.Application.DTOs.Progress;
using AITrainingSystem.Application.Interfaces.Respository;
using AITrainingSystem.Domain.Entities;

namespace AITrainingSystem.Infrastructure.Services.Progress;

public class LessonProgressService
    : ILessonProgressService
{
    private readonly ILessonProgressRepository _repo;

    public LessonProgressService(
        ILessonProgressRepository repo)
    {
        _repo = repo;
    }

    public async Task CompleteLessonAsync(
        Guid userId,
        Guid lessonId)
    {
        var exists = await _repo
            .ExistsAsync(userId, lessonId);

        if (exists)
        {
            throw new Exception(
                "Lesson already completed");
        }

        var progress = new LessonProgress
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            LessonId = lessonId,
            IsCompleted = true,
            CompletedAt = DateTime.UtcNow
        };

        await _repo.AddAsync(progress);
    }

    public async Task<CourseProgressDto>
        GetCourseProgressAsync(
            Guid userId,
            Guid courseId)
    {
        var completedLessons =
            await _repo.GetCompletedCountAsync(
                userId,
                courseId);

        var totalLessons =
            await _repo.GetTotalLessonsAsync(
                courseId);

        double percentage = 0;

        if (totalLessons > 0)
        {
            percentage =
                ((double)completedLessons
                / totalLessons) * 100;
        }

        return new CourseProgressDto
        {
            CourseId = courseId,
            CompletedLessons = completedLessons,
            TotalLessons = totalLessons,
            ProgressPercentage =
                Math.Round(percentage, 2)
        };
    }
}