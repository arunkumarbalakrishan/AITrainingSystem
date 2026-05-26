using AITrainingSystem.Application.Features.Media.DTOs;
using AITrainingSystem.Application.Features.Media.Interfaces;
using AITrainingSystem.Domain.Entities;
using AITrainingSystem.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace AITrainingSystem.Application.Features.Media.Services;

public class MediaService : IMediaService
{
    private readonly ApplicationDbContext _context;

    public MediaService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<MediaResponseDto> UploadAsync(
    UploadMediaRequestDto request)
    {
        var file = request.File;

        var allowedExtensions = new[]
        {
            ".mp4",
            ".pdf"
        };

        var extension = Path.GetExtension(file.FileName)
            .ToLower();

        if (!allowedExtensions.Contains(extension))
        {
            throw new Exception(
                "Only MP4 and PDF files are allowed.");
        }

        const long maxVideoSize = 500 * 1024 * 1024;
        const long maxPdfSize = 20 * 1024 * 1024;

        if (extension == ".mp4" &&
            file.Length > maxVideoSize)
        {
            throw new Exception(
                "Video exceeds 500MB limit.");
        }

        if (extension == ".pdf" &&
            file.Length > maxPdfSize)
        {
            throw new Exception(
                "PDF exceeds 20MB limit.");
        }

        var allowedContentTypes = new[]
        {
            "video/mp4",
            "application/pdf"
        };

        if (!allowedContentTypes.Contains(file.ContentType))
        {
            throw new Exception(
                "Invalid file content type.");
        }

        var lessonExists = await _context.Lessons
             .AnyAsync(x => x.Id == request.LessonId);

        if (!lessonExists)
        {
            throw new Exception("Lesson not found.");
        }

        // Generate unique filename
        var uniqueFileName =
            $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";


        //// Decide folder
        const string videoFolder = "videos";
        const string pdfFolder = "pdfs";

        var folder = file.ContentType == "video/mp4"
            ? videoFolder
            : pdfFolder;

        // Full physical path
        var uploadsFolder = Path.Combine(
            Directory.GetCurrentDirectory(),
            "Storage",
            folder);

        // Create folder if not exists
        if (!Directory.Exists(uploadsFolder))
        {
            Directory.CreateDirectory(uploadsFolder);
        }

        // Full file path
        var filePath = Path.Combine(
            uploadsFolder,
            uniqueFileName);

        // Save file physically
        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        // Create Media entity
        var media = new MediaFile
        {
            Id = Guid.NewGuid(),
            FileName = file.FileName,
            StoredFileName = uniqueFileName,
            FilePath = $"{folder}/{uniqueFileName}",
            FileType = file.ContentType,
            FileSize = file.Length,
            LessonId = request.LessonId,
            UploadedAt = DateTime.UtcNow,
            MediaType = file.ContentType.StartsWith("video")
                ? Domain.Enums.MediaType.Video
                : Domain.Enums.MediaType.Pdf
        };

        // Save to DB
        _context.MediaFiles.Add(media);

        await _context.SaveChangesAsync();

        // Return response
        return new MediaResponseDto
        {
            Id = media.Id,
            FileName = media.FileName,
            FileType = media.FileType,
            FileSize = media.FileSize,
            FilePath = media.FilePath,
            UploadedAt = media.UploadedAt,
            MediaType = media.MediaType.ToString()
        };
    }
}