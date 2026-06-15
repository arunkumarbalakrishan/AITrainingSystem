using AITrainingSystem.Application.DTOs.Progress;
using AITrainingSystem.Application.Interfaces.Services;
using AITrainingSystem.Persistence.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AITrainingSystem.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProgressController : ControllerBase
{
    private readonly ILessonProgressService _service;
    private readonly IProgressService _progressService;
    private readonly ApplicationDbContext _context;

    public ProgressController(ILessonProgressService service, IProgressService progressService, ApplicationDbContext context)
    {
        _service = service;
        _progressService = progressService;
        _context = context;
    }
    [Authorize]
    [HttpPost("complete")]
    public async Task<IActionResult> CompleteLesson(CompleteLessonDto dto)

    {
        var userIdClaim =
            User.FindFirstValue(
                ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userIdClaim))
        {
            return Unauthorized(new
            {
                Success = false,
                Message = "User not authenticated"
            });
        }

        if (!Guid.TryParse(
            userIdClaim,
            out var userId))
        {
            return BadRequest(new
            {
                Success = false,
                Message = "Invalid user ID"
            });
        }

        await _service.CompleteLessonAsync(
            userId,
            dto.LessonId);

        return Ok(new
        {
            Success = true,
            Message = "Lesson marked as completed"
        });
    }
    [Authorize]
    [HttpGet("course/{courseId}")]
    public async Task<IActionResult> GetCourseProgress(Guid courseId)
    {
        var userIdClaim =
            User.FindFirstValue(
                ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userIdClaim))
        {
            return Unauthorized(new
            {
                Success = false,
                Message = "User not authenticated"
            });
        }

        if (!Guid.TryParse(
            userIdClaim,
            out var userId))
        {
            return BadRequest(new
            {
                Success = false,
                Message = "Invalid user ID"
            });
        }

        var result =
            await _service.GetCourseProgressAsync(
                userId,
                courseId);

        return Ok(new
        {
            Success = true,
            Data = result
        });
    }
    [Authorize]
    [HttpGet("{lessonId}")]
    public async Task<IActionResult> GetProgress(Guid lessonId)
    {
        var userId = GetUserId();

        var progress = await _progressService
                .GetVideoProgressAsync(
                    userId,
                    lessonId);

        if (progress == null)
        {
            return NotFound();
        }

        return Ok(progress);
    }
    [Authorize]
    [HttpPost("video")]
    public async Task<IActionResult> UpdateVideoProgress(
    UpdateVideoProgressDto dto)
    {
        var userId = GetUserId();

        await _progressService
            .UpdateVideoProgressAsync(
                userId,
                dto);

        return Ok(new
        {
            Success = true,
            Message = "Video progress updated successfully"
        });
    }

    [Authorize]
    [HttpGet("continue-watching")]
    public async Task<IActionResult> GetContinueWatching()
    {
        var userId = GetUserId();

        var result =
            await _progressService
                .GetContinueWatchingAsync(userId);

        return Ok(new
        {
            Success = true,
            Data = result
        });
    }

    private Guid GetUserId()
    {
        var userId = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        return Guid.Parse(userId!);
    }

    [Authorize]
    [HttpGet("course/{courseId}/completed-lessons")]
    public async Task<IActionResult> GetCompletedLessons(Guid courseId)
    {
        var userId = GetUserId();

        var completedLessonIds = await _context.LessonProgresses
            .Where(lp => lp.UserId == userId && lp.IsCompleted && lp.Lesson.CourseId == courseId)
            .Select(lp => lp.LessonId)
            .ToListAsync();

        return Ok(new
        {
            Success = true,
            Data = completedLessonIds
        });
    }
}