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

    public MediaController(IMediaAccessService mediaAccessService, IMediaService mediaService   )
    {
        _mediaAccessService = mediaAccessService;
        _mediaService = mediaService;
    }

    [Authorize]
    [HttpPost("upload")]
    public async Task<IActionResult>
    Upload([FromForm] UploadMediaRequestDto request)
    {
        var result = await _mediaService
            .UploadAsync(request);

        return Ok(result);
    }

    [HttpGet("video/{lessonId}")]
    public async Task<IActionResult>
      GetLessonVideo(Guid lessonId)
    {
        // STEP 1 — Get Current User ID
        var userId = Guid.Parse(
            User.FindFirst(ClaimTypes.NameIdentifier)!
                .Value);

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
            return NotFound("Video file not found.");
        }

        // STEP 5 — Stream Video Securely
        var stream = new FileStream(
            fullPath,
            FileMode.Open,
            FileAccess.Read);

        return File(
            stream,
            result.ContentType,
            enableRangeProcessing: true);
    }

    [HttpGet("pdf/{lessonId}")]
    public async Task<IActionResult>
    GetLessonPdf(Guid lessonId)
    {
        // STEP 1 — Get Current User ID
        var userId = Guid.Parse(
            User.FindFirst(ClaimTypes.NameIdentifier)!
                .Value);

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
            return NotFound("PDF file not found.");
        }

        // STEP 5 — Return Secure PDF
        var bytes = await System.IO.File
            .ReadAllBytesAsync(fullPath);

        return File(
            bytes,
            result.ContentType,
            result.FileName);
    }
}