using AITrainingSystem.Application.Features.Media.DTOs;
using AITrainingSystem.Application.Features.Media.Interfaces;
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
    private readonly ILogger<MediaController> _logger;

    public MediaController(IMediaAccessService mediaAccessService, IMediaService mediaService, ILogger<MediaController> logger)
    {
        _mediaAccessService = mediaAccessService;
        _mediaService = mediaService;
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


        // STEP 3 — Build Full Physical Path
        var fullPath = Path.Combine(
            Directory.GetCurrentDirectory(),
            "Storage",
            result.FilePath);

        // STEP 4 — Validate File Exists
        if (!System.IO.File.Exists(fullPath))
        {
            _logger.LogWarning("Video file missing for Lesson {LessonId}", lessonId);
            return NotFound("Video file not found.");
        }

        // STEP 5 — Stream Video Securely
        var stream = new FileStream(
         fullPath,
         FileMode.Open,
         FileAccess.Read,
         FileShare.Read,
         1024 * 64,
         useAsync: true);

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

        // STEP 3 — Build Physical Path
        var fullPath = Path.Combine(
            Directory.GetCurrentDirectory(),
            "Storage",
            result.FilePath);

        // STEP 4 — Check File Exists
        if (!System.IO.File.Exists(fullPath))
        {
            _logger.LogWarning("PDF file missing for Lesson {LessonId}", lessonId);
            return NotFound("PDF file not found.");
        }

        // STEP 5 — Return Secure PDF
        var stream = new FileStream(
            fullPath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            1024 * 64,
            useAsync: true);

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