using AITrainingSystem.Application.DTOs.Analytics;
using AITrainingSystem.Application.Interfaces.Respository;
using AITrainingSystem.Domain.Entities;
using AITrainingSystem.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace AITrainingSystem.Persistence.Repositories;

public class LessonProgressRepository : ILessonProgressRepository
{
    private readonly ApplicationDbContext _context;

    public LessonProgressRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> ExistsAsync(Guid userId, Guid lessonId)
    {
        return await _context.LessonProgresses
            .AnyAsync(x =>
                x.UserId == userId &&
                x.LessonId == lessonId);
    }

    public async Task AddAsync(LessonProgress progress)
    {
        await _context.LessonProgresses
            .AddAsync(progress);

        await _context.SaveChangesAsync();
    }

    public async Task<int> GetCompletedCountAsync(
        Guid userId,
        Guid courseId)
    {
        return await _context.LessonProgresses
            .CountAsync(x =>
                x.UserId == userId &&
                x.IsCompleted &&
                x.Lesson.CourseId == courseId);
    }

    public async Task<int> GetTotalLessonsAsync(Guid courseId)
    {
        return await _context.Lessons
            .CountAsync(x => x.CourseId == courseId);
    }
    public async Task<int> GetCompletedCourseCountAsync(Guid userId)
    {
        var completedCourses = await _context.Courses
            .CountAsync(course =>
                course.Lessons.Count > 0 &&
                course.Lessons.All(lesson =>
                    lesson.Progresses.Any(progress =>
                        progress.UserId == userId &&
                        progress.IsCompleted)));

        return completedCourses;
    }
    public async Task<List<RecentlyCompletedCourseDto>> GetRecentlyCompletedCoursesAsync(Guid userId)
    {
        return await _context.LessonProgresses
            .Where(x =>
                x.UserId == userId &&
                x.IsCompleted)
            .OrderByDescending(x => x.CompletedAt)
            .Select(x => new RecentlyCompletedCourseDto
            {
                CourseId = x.Lesson!.CourseId,
                CourseTitle = x.Lesson.Course.Title,
                CompletedAt = x.CompletedAt
            })
            .Distinct()
            .Take(10)
            .ToListAsync();
    }
}