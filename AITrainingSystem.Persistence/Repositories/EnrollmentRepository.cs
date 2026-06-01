using AITrainingSystem.Application.Interfaces.Repositories;
using AITrainingSystem.Domain.Entities;
using AITrainingSystem.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace AITrainingSystem.Persistence.Repositories;

public class EnrollmentRepository : IEnrollmentRepository
{
    private readonly ApplicationDbContext _context;

    public EnrollmentRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> IsUserEnrolledAsync(Guid userId, Guid courseId)
    {
        return await _context.Enrollments
            .AnyAsync(e => e.UserId == userId && e.CourseId == courseId);
    }

    public async Task AddEnrollmentAsync(Enrollment enrollment)
    {
        await _context.Enrollments.AddAsync(enrollment);
    }

    public async Task<Enrollment?> GetEnrollmentAsync(Guid userId, Guid courseId)
    {
        return await _context.Enrollments
            .FirstOrDefaultAsync(e =>
                e.UserId == userId &&
                e.CourseId == courseId);
    }

    public async Task<int> GetEnrolledCourseCountAsync(Guid userId)
    {
        return await _context.Enrollments
            .CountAsync(x => x.UserId == userId);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}