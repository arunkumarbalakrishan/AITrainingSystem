using AITrainingSystem.Application.DTOs.Lesson;
using AITrainingSystem.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AITrainingSystem.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LessonController : ControllerBase
{
    private readonly ILessonService _service;

    public LessonController(ILessonService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateLessonDto dto)
    {
        var id = await _service.CreateAsync(dto);

        return Ok(new
        {
            Success = true,
            Message = "Lesson created successfully",
            Data = new
            {
                Id = id
            }
        });
    }

    [HttpGet("course/{courseId}")]
    public async Task<IActionResult> GetByCourse(Guid courseId)
    {
        var result = await _service.GetByCourseIdAsync(courseId);

        return Ok(new
        {
            Success = true,
            Count = result.Count,
            Data = result
        });
    }

    [Authorize]
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userIdClaim))
        {
            return Unauthorized(new
            {
                Success = false,
                Message = "User not authenticated"
            });
        }

        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            return BadRequest(new
            {
                Success = false,
                Message = "Invalid user ID"
            });
        }

        try
        {
            var result = await _service.GetByIdAsync(id, userId);

            return Ok(new
            {
                Success = true,
                Data = result
            });
        }
        catch (Exception ex)
        {
            return NotFound(new
            {
                Success = false,
                Message = ex.Message
            });
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, UpdateLessonDto dto)
    {
        var result = await _service.UpdateAsync(id, dto);

        if (!result)
        {
            return NotFound(new
            {
                Success = false,
                Message = "Lesson not found"
            });
        }

        return Ok(new
        {
            Success = true,
            Message = "Lesson updated successfully"
        });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _service.DeleteAsync(id);

        if (!result)
        {
            return NotFound(new
            {
                Success = false,
                Message = "Lesson not found"
            });
        }

        return Ok(new
        {
            Success = true,
            Message = "Lesson deleted successfully"
        });
    }
}