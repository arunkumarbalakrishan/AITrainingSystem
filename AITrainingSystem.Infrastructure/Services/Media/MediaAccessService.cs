using AITrainingSystem.Application.Features.Media.DTOs;
using AITrainingSystem.Application.Features.Media.Interfaces;
using AITrainingSystem.Domain.Entities;
using AITrainingSystem.Domain.Enums;
using AITrainingSystem.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace AITrainingSystem.Application.Features.Media.Services;

public class MediaAccessService : IMediaAccessService
{
    private readonly ApplicationDbContext _context;

    public MediaAccessService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<MediaAccessResponseDto>
        GetLessonVideoAsync(
            Guid lessonId,
            Guid userId)
    {
        // STEP 1 — Validate Lesson
        var lesson = await _context.Lessons
            .FirstOrDefaultAsync(x => x.Id == lessonId);

        if (lesson == null)
        {
            throw new Exception("Lesson not found.");
        }

        // STEP 2 — Validate Enrollment or Free Preview
        var isEnrolled = await _context.Enrollments
            .AnyAsync(x =>
                x.CourseId == lesson.CourseId &&
                x.UserId == userId);

        if (!isEnrolled && !lesson.IsPreviewFree)
        {
            throw new UnauthorizedAccessException(
                "You are not enrolled in this course and this lesson is not a free preview.");
        }

        // STEP 3 — Get Video Media
        var media = await _context.MediaFiles
            .FirstOrDefaultAsync(x =>
                x.LessonId == lessonId &&
                x.MediaType == MediaType.Video);

        if (media == null)
        {
            throw new Exception("Video not found.");
        }

        // STEP 4 — Return Secure Media Info
        return new MediaAccessResponseDto
        {
            FilePath = media.FilePath,
            FileName = media.FileName,
            ContentType = media.FileType,
            FileSize = media.FileSize
        };
    }

    public async Task<MediaAccessResponseDto>
        GetLessonPdfAsync(
            Guid lessonId,
            Guid userId)
    {
        // STEP 1 — Validate Lesson
        var lesson = await _context.Lessons
            .FirstOrDefaultAsync(x => x.Id == lessonId);

        if (lesson == null)
        {
            throw new Exception("Lesson not found.");
        }

        // STEP 2 — Validate Enrollment or Free Preview
        var isEnrolled = await _context.Enrollments
            .AnyAsync(x =>
                x.CourseId == lesson.CourseId &&
                x.UserId == userId);

        if (!isEnrolled && !lesson.IsPreviewFree)
        {
            throw new UnauthorizedAccessException(
                "You are not enrolled in this course and this lesson is not a free preview.");
        }

        // STEP 3 — Get PDF Media
        var media = await _context.MediaFiles
            .FirstOrDefaultAsync(x =>
                x.LessonId == lessonId &&
                x.MediaType == MediaType.Pdf);

        if (media == null)
        {
            throw new Exception("PDF not found.");
        }

        // STEP 4 — Return Secure Media Info
        return new MediaAccessResponseDto
        {
            FilePath = media.FilePath,
            FileName = media.FileName,
            ContentType = media.FileType,
            FileSize = media.FileSize
        };
    }
    public async Task SaveVideoProgressAsync(
    Guid userId,
    SaveVideoProgressDto dto)
    {
        // STEP 1 — Check Existing Progress
        var existingProgress = await _context.VideoProgresses
            .FirstOrDefaultAsync(x =>
                x.UserId == userId &&
                x.LessonId == dto.LessonId);

        // STEP 2 — If Progress Exists → Update
        if (existingProgress != null)
        {
            existingProgress.LastWatchedSecond =
                dto.LastWatchedSecond;

            existingProgress.WatchPercentage =
                dto.WatchPercentage;

            existingProgress.IsCompleted =
                dto.WatchPercentage >= 90;

            existingProgress.UpdatedAt =
                DateTime.UtcNow;
        }

        // STEP 3 — Else Create New Progress
        else
        {
            var progress = new VideoProgress
            {
                Id = Guid.NewGuid(),

                UserId = userId,

                LessonId = dto.LessonId,

                LastWatchedSecond =
                    dto.LastWatchedSecond,

                WatchPercentage =
                    dto.WatchPercentage,

                IsCompleted =
                    dto.WatchPercentage >= 90,

                UpdatedAt = DateTime.UtcNow
            };

            await _context.VideoProgresses
                .AddAsync(progress);
        }

        // STEP 4 — Save Changes
        await _context.SaveChangesAsync();
    }
}