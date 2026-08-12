using AITrainingSystem.Application.DTOs.Analytics;
using AITrainingSystem.Application.Interfaces.DashboardService;
using AITrainingSystem.Application.Interfaces.Repositories;
using AITrainingSystem.Application.Interfaces.Respository;
using AITrainingSystem.Application.Interfaces.Services;
using AITrainingSystem.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AITrainingSystem.Infrastructure.Services.Dashboard;

public class DashboardService : IDashboardService
{
    private readonly IEnrollmentRepository _enrollmentRepository;
    private readonly ILessonProgressRepository _progressRepository;
    private readonly ICertificateRepository _certificateRepository;
    private readonly ApplicationDbContext _context;
    private readonly ICacheService _cacheService;

    public DashboardService(
        IEnrollmentRepository enrollmentRepository,
        ILessonProgressRepository progressRepository,
        ICertificateRepository certificateRepository,
        ApplicationDbContext context,
        ICacheService cacheService)
    {
        _enrollmentRepository = enrollmentRepository;
        _progressRepository = progressRepository;
        _certificateRepository = certificateRepository;
        _context = context;
        _cacheService = cacheService;
    }

    public async Task<LearningAnalyticsDto>
        GetAnalyticsAsync(Guid userId)
    {
        var cacheKey = $"user:{userId}:dashboard";
        var cached = await _cacheService.GetAsync<LearningAnalyticsDto>(cacheKey);
        if (cached != null)
        {
            return cached;
        }

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

        // Calculate active learning streak dynamically
        var lessonDates = (await _context.LessonProgresses
            .Where(lp => lp.UserId == userId && lp.IsCompleted)
            .Select(lp => lp.CompletedAt)
            .ToListAsync())
            .Select(d => d.Date);

        var interviewDates = (await _context.MockInterviewSessions
            .Where(mis => mis.UserId == userId)
            .Select(mis => mis.CreatedAt)
            .ToListAsync())
            .Select(d => d.Date);

        var videoDates = (await _context.VideoProgresses
            .Where(vp => vp.UserId == userId)
            .Select(vp => vp.UpdatedAt)
            .ToListAsync())
            .Select(d => d.Date);

        var assessmentDates = (await _context.AssessmentResults
            .Where(ar => ar.UserId == userId)
            .Select(ar => ar.SubmittedAt)
            .ToListAsync())
            .Select(d => d.Date);

        var allDates = lessonDates
            .Union(interviewDates)
            .Union(videoDates)
            .Union(assessmentDates)
            .Distinct()
            .OrderByDescending(d => d)
            .ToList();

        int streak = 0;
        if (allDates.Any())
        {
            var today = DateTime.UtcNow.Date;
            var checkDate = today;

            // If not active today, check if yesterday was active to keep streak alive
            if (!allDates.Contains(today))
            {
                checkDate = today.AddDays(-1);
            }

            while (allDates.Contains(checkDate))
            {
                streak++;
                checkDate = checkDate.AddDays(-1);
            }
        }

        var result = new LearningAnalyticsDto
        {
            TotalCoursesEnrolled = totalCourses,
            CompletedCourses = completedCourses,
            InProgressCourses = totalCourses - completedCourses,
            CertificatesEarned = certificatesCount,
            TotalHours = totalHours,
            StreakDays = streak
        };

        await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(5));
        return result;
    }

    public async Task<List<RecentlyCompletedCourseDto>>
    GetRecentlyCompletedCoursesAsync(Guid userId)
    {
        var cacheKey = $"user:{userId}:recent_courses";
        var cached = await _cacheService.GetAsync<List<RecentlyCompletedCourseDto>>(cacheKey);
        if (cached != null)
        {
            return cached;
        }

        var result = await _progressRepository
            .GetRecentlyCompletedCoursesAsync(userId);

        await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(5));
        return result;
    }

    public async Task<AdminReportsDto> GetAdminReportsAsync()
    {
        var cacheKey = "admin:reports";
        var cached = await _cacheService.GetAsync<AdminReportsDto>(cacheKey);
        if (cached != null)
        {
            return cached;
        }

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

        var result = new AdminReportsDto
        {
            TotalRevenue = totalRevenue,
            MonthlyRevenue = monthlyRevenue,
            RecentTransactions = recentTransactions,
            StudentProgress = studentProgressList,
            TopCourses = topCourses,
            IssuedCertificates = issuedCertificatesList
        };

        await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(3));
        return result;
    }
}