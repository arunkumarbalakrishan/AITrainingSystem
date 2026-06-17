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

    public async Task<AdminReportsDto> GetAdminReportsAsync()
    {
        // Retroactively update completed mock payments that were saved with Amount == 0
        var zeroPayments = await _context.Payments
            .Include(p => p.Course)
            .Where(p => p.Amount == 0 && p.Status == "Completed" && p.Course != null)
            .ToListAsync();
        if (zeroPayments.Any())
        {
            foreach (var p in zeroPayments)
            {
                p.Amount = p.Course.Price;
            }
            await _context.SaveChangesAsync();
        }

        var completedPayments = await _context.Payments
            .Where(p => p.Status == "Completed")
            .ToListAsync();

        var totalRevenue = completedPayments.Sum(p => p.Amount);

        var monthlyRevenue = completedPayments
            .GroupBy(p => new { p.CreatedAt.Year, p.CreatedAt.Month })
            .Select(g => new MonthlyRevenueDto
            {
                Month = $"{g.Key.Year}-{g.Key.Month:D2}",
                Revenue = g.Sum(p => p.Amount)
            })
            .OrderBy(m => m.Month)
            .ToList();

        var recentTransactions = await _context.Payments
            .Include(p => p.User)
            .Include(p => p.Course)
            .OrderByDescending(p => p.CreatedAt)
            .Take(50)
            .Select(p => new TransactionDto
            {
                PaymentId = p.Id,
                StudentName = p.User != null ? p.User.FullName : "Unknown",
                StudentEmail = p.User != null ? p.User.Email : "Unknown",
                CourseTitle = p.Course != null ? p.Course.Title : "Unknown",
                Amount = p.Amount,
                Currency = p.Currency,
                Status = p.Status,
                Date = p.CreatedAt
            })
            .ToListAsync();

        var enrollments = await _context.Enrollments
            .Include(e => e.User)
            .Include(e => e.Course)
            .ThenInclude(c => c.Lessons)
            .ToListAsync();

        var completedProgress = await _context.LessonProgresses
            .Include(lp => lp.Lesson)
            .Where(lp => lp.IsCompleted)
            .ToListAsync();

        var certificates = await _context.Certificates
            .Include(c => c.User)
            .Include(c => c.Course)
            .ToListAsync();

        var studentProgressList = new List<StudentProgressReportDto>();
        foreach (var e in enrollments)
        {
            if (e.User == null || e.Course == null) continue;

            var totalLessons = e.Course.Lessons?.Count ?? 0;
            var completedCount = completedProgress
                .Count(lp => lp.UserId == e.UserId && lp.Lesson?.CourseId == e.CourseId);

            var percent = totalLessons > 0 ? Math.Round((double)completedCount / totalLessons * 100, 2) : 0;
            var hasCert = certificates.Any(c => c.UserId == e.UserId && c.CourseId == e.CourseId);

            studentProgressList.Add(new StudentProgressReportDto
            {
                StudentId = e.UserId,
                StudentName = e.User.FullName,
                StudentEmail = e.User.Email,
                CourseTitle = e.Course.Title,
                CompletedLessonsCount = completedCount,
                TotalLessonsCount = totalLessons,
                ProgressPercentage = percent,
                HasCertificate = hasCert
            });
        }

        var courses = await _context.Courses.ToListAsync();
        var allEnrollments = await _context.Enrollments.ToListAsync();

        var topCourses = courses.Select(c => new TopCourseReportDto
        {
            CourseId = c.Id,
            CourseTitle = c.Title,
            EnrollmentCount = allEnrollments.Count(e => e.CourseId == c.Id),
            TotalRevenueGenerated = completedPayments.Where(p => p.CourseId == c.Id).Sum(p => p.Amount),
            Price = c.Price,
            IsPublished = c.IsPublished
        })
        .OrderByDescending(tc => tc.EnrollmentCount)
        .ThenByDescending(tc => tc.TotalRevenueGenerated)
        .ToList();

        var issuedCertificatesList = certificates.Select(c => new AdminCertificateDto
        {
            CertificateId = c.Id,
            CertificateNumber = c.CertificateNumber,
            StudentName = c.User != null ? c.User.FullName : "Unknown",
            StudentEmail = c.User != null ? c.User.Email : "Unknown",
            CourseTitle = c.Course != null ? c.Course.Title : "Unknown",
            IssuedDate = c.IssuedDate
        })
        .OrderByDescending(c => c.IssuedDate)
        .ToList();

        return new AdminReportsDto
        {
            TotalRevenue = totalRevenue,
            MonthlyRevenue = monthlyRevenue,
            RecentTransactions = recentTransactions,
            StudentProgress = studentProgressList,
            TopCourses = topCourses,
            IssuedCertificates = issuedCertificatesList
        };
    }
}