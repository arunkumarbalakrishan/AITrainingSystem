using AITrainingSystem.Application.Features.Media.DTOs;
using AITrainingSystem.Application.Features.Media.Interfaces;
using AITrainingSystem.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AITrainingSystem.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MediaController : ControllerBase
{
    private readonly IMediaAccessService _mediaAccessService;
    private readonly IMediaService _mediaService;
    private readonly IStorageService _storageService;
    private readonly ILogger<MediaController> _logger;

    public MediaController(IMediaAccessService mediaAccessService, IMediaService mediaService, IStorageService storageService, ILogger<MediaController> logger)
    {
        _mediaAccessService = mediaAccessService;
        _mediaService = mediaService;
        _storageService = storageService;
        _logger = logger;
    }

    [Authorize]
    [HttpPost("upload")]
    public async Task<IActionResult>Upload([FromForm] UploadMediaRequestDto request)
    {
        var result = await _mediaService
            .UploadAsync(request);

        return Ok(result);
    }

    [Authorize]
    [HttpGet("video/{lessonId}")]
    public async Task<IActionResult>GetLessonVideo(Guid lessonId)
    {
        // STEP 1 — Get Current User ID
        var userId = Guid.Parse(
            User.FindFirst(ClaimTypes.NameIdentifier)!
                .Value);

        _logger.LogInformation("User {UserId} requested video for Lesson {LessonId}", userId, lessonId);

        // STEP 2 — Validate Secure Access
        var result = await _mediaAccessService
            .GetLessonVideoAsync(
                lessonId,
                userId);

        // STEP 3 & 4 — Stream Video using Storage Service
        Stream stream;
        try
        {
            stream = await _storageService.GetFileStreamAsync(result.FilePath);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Video file missing or inaccessible for Lesson {LessonId}", lessonId);
            return NotFound("Video file not found.");
        }

        _logger.LogInformation("Video streamed successfully for Lesson {LessonId}", lessonId);

        return File(
            stream,
            result.ContentType,
            enableRangeProcessing: true);
    }

    [Authorize]
    [HttpGet("pdf/{lessonId}")]
    public async Task<IActionResult>GetLessonPdf(Guid lessonId)
    {
        // STEP 1 — Get Current User ID
        var userId = Guid.Parse(
            User.FindFirst(ClaimTypes.NameIdentifier)!
                .Value);
        _logger.LogInformation("User {UserId} requested PDF for Lesson {LessonId}", userId, lessonId);

        // STEP 2 — Validate Access
        var result = await _mediaAccessService
            .GetLessonPdfAsync(
                lessonId,
                userId);

        // STEP 3 & 4 — Stream PDF using Storage Service
        Stream stream;
        try
        {
            stream = await _storageService.GetFileStreamAsync(result.FilePath);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "PDF file missing or inaccessible for Lesson {LessonId}", lessonId);
            return NotFound("PDF file not found.");
        }

        _logger.LogInformation("PDF streamed successfully for Lesson {LessonId}", lessonId);

        return File(
            stream,
            result.ContentType,
            result.FileName);
    }

    [Authorize]
    [HttpPost("progress")]
    public async Task<IActionResult>SaveVideoProgress(SaveVideoProgressDto dto)
    {
        var userId = Guid.Parse(
            User.FindFirst(ClaimTypes.NameIdentifier)!
            .Value);

        if (dto.WatchPercentage < 0 || dto.WatchPercentage > 100)
        {
            throw new Exception("Invalid watch percentage.");
        }

        if (dto.LastWatchedSecond < 0)
        {
            throw new Exception("Invalid video timestamp.");
        }

        await _mediaAccessService
            .SaveVideoProgressAsync(
                userId,
                dto);

        return Ok(new
        {
            success = true,
            message = "Video progress saved."
        });
    }
}