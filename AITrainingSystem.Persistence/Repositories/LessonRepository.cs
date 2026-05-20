using AITrainingSystem.Application.Interfaces.Lessons;
using AITrainingSystem.Domain.Entities;
using AITrainingSystem.Persistence.Context;
using Microsoft.EntityFrameworkCore;

public class LessonRepository : ILessonRepository
{
    private readonly ApplicationDbContext _context;

    public LessonRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Lesson?> GetByIdAsync(Guid id)
    {
        return await _context.Lessons
            .Include(x => x.Course)
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<List<Lesson>> GetByCourseIdAsync(Guid courseId)
    {
        return await _context.Lessons
            .Where(x => x.CourseId == courseId)
            .OrderBy(x => x.Order)
            .ToListAsync();
    }

    public async Task AddAsync(Lesson lesson)
    {
        await _context.Lessons.AddAsync(lesson);
    }

    public Task UpdateAsync(Lesson lesson)
    {
        _context.Lessons.Update(lesson);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Lesson lesson)
    {
        _context.Lessons.Remove(lesson);
        return Task.CompletedTask;
    }
    public async Task<int> GetNextOrderAsync(Guid courseId)
    {
        var lastOrder = await _context.Lessons
            .Where(x => x.CourseId == courseId)
            .OrderByDescending(x => x.Order)
            .Select(x => x.Order)
            .FirstOrDefaultAsync();

        return lastOrder + 1;
    }
    public async Task<bool> OrderExistsAsync(Guid courseId, int order)
    {
        return await _context.Lessons
            .AnyAsync(x => x.CourseId == courseId && x.Order == order);

    }
}