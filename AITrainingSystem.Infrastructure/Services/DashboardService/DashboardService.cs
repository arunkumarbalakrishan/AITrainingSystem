using AITrainingSystem.Application.DTOs.Analytics;
using AITrainingSystem.Application.Interfaces.DashboardService;
using AITrainingSystem.Application.Interfaces.Repositories;
using AITrainingSystem.Application.Interfaces.Respository;
using AITrainingSystem.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace AITrainingSystem.Infrastructure.Services.Dashboard;

public class DashboardService : IDashboardService
{
    private readonly IEnrollmentRepository _enrollmentRepository;
    private readonly ILessonProgressRepository _progressRepository;
    private readonly ICertificateRepository _certificateRepository;
    private readonly ApplicationDbContext _context;

    public DashboardService(
        IEnrollmentRepository enrollmentRepository,
        ILessonProgressRepository progressRepository,
        ICertificateRepository certificateRepository,
        ApplicationDbContext context)
    {
        _enrollmentRepository = enrollmentRepository;
        _progressRepository = progressRepository;
        _certificateRepository = certificateRepository;
        _context = context;
    }

    public async Task<LearningAnalyticsDto>
        GetAnalyticsAsync(Guid userId)
    {
        var totalCourses =
            await _enrollmentRepository
                .GetEnrolledCourseCountAsync(userId);

        var completedCourses =
            await _progressRepository
                .GetCompletedCourseCountAsync(userId);

        var certificates = await _certificateRepository
            .GetByUserIdAsync(userId);
        var certificatesCount = certificates.Count();

        var totalHours = await _context.Enrollments
            .Where(e => e.UserId == userId)
            .Select(e => e.Course.DurationInHours)
            .SumAsync();

        return new LearningAnalyticsDto
        {
            TotalCoursesEnrolled = totalCourses,
            CompletedCourses = completedCourses,
            InProgressCourses = totalCourses - completedCourses,
            CertificatesEarned = certificatesCount,
            TotalHours = totalHours
        };
    }
    public async Task<List<RecentlyCompletedCourseDto>>
    GetRecentlyCompletedCoursesAsync(Guid userId)
    {
        return await _progressRepository
            .GetRecentlyCompletedCoursesAsync(userId);
    }
}