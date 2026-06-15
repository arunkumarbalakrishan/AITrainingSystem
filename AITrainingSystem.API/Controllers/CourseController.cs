using AITrainingSystem.Application.Common.Models;
using AITrainingSystem.Application.DTOs.Course;
using AITrainingSystem.Application.Interfaces.Services;
using AITrainingSystem.Persistence.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AITrainingSystem.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CourseController : ControllerBase
{
    private readonly ICourseService _courseService;
    private readonly ApplicationDbContext _context;

    public CourseController(ICourseService courseService, ApplicationDbContext context)
    {
        _courseService = courseService;
        _context = context;
    }

    [HttpPost]
    public async Task<IActionResult> CreateCourse(CreateCourseDto dto)
    {
        var response = await _courseService.CreateAsync(dto);

        return Ok(response);
    }

    [HttpGet]
    public async Task<IActionResult> GetAllCourses()
    {
        var response = await _courseService.GetAllAsync();

        return Ok(response);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetCourseById(Guid id)
    {
        var response = await _courseService.GetByIdAsync(id);

        if (!response.Success)
        {
            return NotFound(response);
        }

        return Ok(response);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateCourse(Guid id, UpdateCourseDto dto)
    {
        var response = await _courseService.UpdateAsync(id, dto);

        if (!response.Success)
        {
            return NotFound(response);
        }

        return Ok(response);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCourse(Guid id)
    {
        var response = await _courseService.DeleteAsync(id);

        if (!response.Success)
        {
            return NotFound(response);
        }

        return Ok(new { Success = true, Message = response.Data });
    }

    [Authorize]
    [HttpGet("{id}/full")]
    public async Task<IActionResult> GetFull(Guid id)
    {
        var userIdClaim =
            User.FindFirstValue(ClaimTypes.NameIdentifier);

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

        var result = await _courseService
            .GetCourseFullAsync(id, userId);

        if (result == null)
        {
            return NotFound(new
            {
                Success = false,
                Message = "Course not found"
            });
        }

        return Ok(new
        {
            Success = true,
            Data = result
        });
    }

    [Authorize]
    [HttpGet("enrolled")]
    public async Task<IActionResult> GetEnrolledCourses()
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

        var enrolledCourses = await _context.Enrollments
            .Where(e => e.UserId == userId)
            .Select(e => e.Course)
            .OrderByDescending(c => c.CreatedAt)
            .Select(x => new CourseResponseDto
            {
                Id = x.Id,
                Title = x.Title,
                Description = x.Description,
                Price = x.Price,
                DurationInHours = x.DurationInHours,
                ThumbnailUrl = x.ThumbnailUrl,
                IsPublished = x.IsPublished,
                CreatedAt = x.CreatedAt
            })
            .ToListAsync();

        return Ok(ApiResponse<IEnumerable<CourseResponseDto>>.SuccessResponse(
            enrolledCourses,
            "Enrolled courses fetched successfully"
        ));
    }
}