using AITrainingSystem.Application.DTOs.Analytics;
using AITrainingSystem.Application.Interfaces.DashboardService;
using AITrainingSystem.Application.Interfaces.Repositories;
using AITrainingSystem.Application.Interfaces.Respository;

namespace AITrainingSystem.Infrastructure.Services.Dashboard;

public class DashboardService : IDashboardService
{
    private readonly IEnrollmentRepository _enrollmentRepository;
    private readonly ILessonProgressRepository _progressRepository;

    public DashboardService(
        IEnrollmentRepository enrollmentRepository,
        ILessonProgressRepository progressRepository)
    {
        _enrollmentRepository = enrollmentRepository;
        _progressRepository = progressRepository;
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

        return new LearningAnalyticsDto
        {
            TotalCoursesEnrolled = totalCourses,
            CompletedCourses = completedCourses,
            InProgressCourses =
                totalCourses - completedCourses
        };
    }
    public async Task<List<RecentlyCompletedCourseDto>>
    GetRecentlyCompletedCoursesAsync(Guid userId)
    {
        return await _progressRepository
            .GetRecentlyCompletedCoursesAsync(userId);
    }
}