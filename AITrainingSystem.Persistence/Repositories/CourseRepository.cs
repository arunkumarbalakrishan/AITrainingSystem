using AITrainingSystem.Application.DTOs.Course;
using AITrainingSystem.Application.Interfaces.Repositories;
using AITrainingSystem.Domain.Entities;
using AITrainingSystem.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace AITrainingSystem.Persistence.Repositories;

public class CourseRepository : ICourseRepository
{
    private readonly ApplicationDbContext _context;

    public CourseRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Course> CreateAsync(Course course)
    {
        await _context.Courses.AddAsync(course);

        await _context.SaveChangesAsync();

        return course;
    }

    public async Task<IEnumerable<Course>> GetAllAsync()
    {
        return await _context.Courses
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();
    }

    public async Task<Course?> GetByIdAsync(Guid id)
    {
        return await _context.Courses
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task UpdateAsync(Course course)
    {
        _context.Courses.Update(course);

        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Course course)
    {
        _context.Courses.Remove(course);

        await _context.SaveChangesAsync();
    }

    public async Task<bool> ExistsAsync(Guid id)
    {
        return await _context.Courses
            .AnyAsync(x => x.Id == id);
    }
    public async Task<CourseFullDto?> GetCourseFullOptimizedAsync(Guid courseId, Guid userId)
    {
        var isEnrolled = await _context.Enrollments
            .AnyAsync(x => x.UserId == userId && x.CourseId == courseId);

        return await _context.Courses
            .Where(c => c.Id == courseId)
            .Select(c => new CourseFullDto
            {
                Id = c.Id,
                Title = c.Title,

                Lessons = c.Lessons
                    .OrderBy(l => l.Order)
                    .Select(l => new LessonAccessDto
                    {
                        Id = l.Id,
                        Title = l.Title,
                        IsPreviewFree = l.IsPreviewFree,
                        IsLocked = !isEnrolled && !l.IsPreviewFree,

                        VideoUrl = (isEnrolled || l.IsPreviewFree)
                            ? l.VideoKey
                            : null,

                        PdfUrl = (isEnrolled || l.IsPreviewFree)
                            ? l.PdfKey
                            : null
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync();
    }
}