using AITrainingSystem.Application.DTOs.Progress;
using AITrainingSystem.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AITrainingSystem.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProgressController : ControllerBase
{
    private readonly ILessonProgressService _service;

    public ProgressController(
        ILessonProgressService service)
    {
        _service = service;
    }

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

    [HttpGet("course/{courseId}")]
    public async Task<IActionResult>
        GetCourseProgress(Guid courseId)
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
}