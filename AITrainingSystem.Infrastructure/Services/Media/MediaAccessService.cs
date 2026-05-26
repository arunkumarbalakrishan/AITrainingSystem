using AITrainingSystem.Application.Features.Media.DTOs;
using AITrainingSystem.Application.Features.Media.Interfaces;
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

        // STEP 2 — Validate Enrollment
        var isEnrolled = await _context.Enrollments
            .AnyAsync(x =>
                x.CourseId == lesson.CourseId &&
                x.UserId == userId);

        if (!isEnrolled)
        {
            throw new UnauthorizedAccessException(
                "You are not enrolled in this course.");
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

        // STEP 2 — Validate Enrollment
        var isEnrolled = await _context.Enrollments
            .AnyAsync(x =>
                x.CourseId == lesson.CourseId &&
                x.UserId == userId);

        if (!isEnrolled)
        {
            throw new UnauthorizedAccessException(
                "You are not enrolled in this course.");
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
}