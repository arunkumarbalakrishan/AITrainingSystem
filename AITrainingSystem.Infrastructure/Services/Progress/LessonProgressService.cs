using AITrainingSystem.Application.DTOs.Analytics;
using AITrainingSystem.Application.DTOs.Progress;
using AITrainingSystem.Application.Interfaces.Repositories;
using AITrainingSystem.Application.Interfaces.Respository;
using AITrainingSystem.Application.Interfaces.Services;
using AITrainingSystem.Domain.Entities;
using AITrainingSystem.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace AITrainingSystem.Infrastructure.Services.Progress;

public class LessonProgressService : ILessonProgressService
{
    private readonly ILessonProgressRepository _repo;
    private readonly ICertificateService _certificateService;
    private readonly IQuizRepository _quizRepo;
    private readonly IAssessmentResultRepository _resultRepo;
    private readonly ApplicationDbContext _context;

    public LessonProgressService(
        ILessonProgressRepository repo,
        ICertificateService certificateService,
        IQuizRepository quizRepo,
        IAssessmentResultRepository resultRepo,
        ApplicationDbContext context)
    {
        _repo = repo;
        _certificateService = certificateService;
        _quizRepo = quizRepo;
        _resultRepo = resultRepo;
        _context = context;
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

    public async Task<CourseProgressDto>GetCourseProgressAsync(Guid userId, Guid courseId)
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
                ((double)completedLessons /
                 totalLessons) * 100;
        }

        var isCompleted =
     totalLessons > 0 &&
     completedLessons == totalLessons;

        // Check if final assessment is passed
        var finalQuiz = await _quizRepo.GetFinalQuizByCourseIdAsync(courseId);
        var hasPassedFinal = true;
        if (finalQuiz != null)
        {
            var bestResult = await _resultRepo.GetBestResultAsync(userId, finalQuiz.Id);
            hasPassedFinal = bestResult != null && bestResult.IsPassed;
        }

        var isEligibleForCertificate = isCompleted && hasPassedFinal;

        if (isEligibleForCertificate)
        {
            await _certificateService.GenerateCertificateAsync(
                userId,
                courseId);
        }

        var hasVideoProgress = await _context.VideoProgresses
            .AnyAsync(vp => vp.UserId == userId && vp.LastWatchedSecond > 0 && vp.Lesson.CourseId == courseId);

        var hasStarted = completedLessons > 0 || hasVideoProgress;

        return new CourseProgressDto
        {
            CourseId = courseId,
            CompletedLessons = completedLessons,
            TotalLessons = totalLessons,
            ProgressPercentage = Math.Round(percentage, 2),
            IsCourseCompleted = isCompleted,
            IsCertificateEligible = isEligibleForCertificate,
            HasStarted = hasStarted
        };
    }

   
}