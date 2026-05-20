using AITrainingSystem.Application.DTOs.Enrollment;
using AITrainingSystem.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AITrainingSystem.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EnrollmentController : ControllerBase
{
    private readonly IEnrollmentService _enrollmentService;

    public EnrollmentController(IEnrollmentService enrollmentService)
    {
        _enrollmentService = enrollmentService;
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Enroll(CreateEnrollmentDto dto)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

        if (userIdClaim is null)
        {
            return Unauthorized();
        }

        var userId = Guid.Parse(userIdClaim.Value);

        await _enrollmentService.EnrollUserAsync(userId, dto.CourseId);

        return Ok(new
        {
            Success = true,
            Message = "Successfully enrolled in course"
        });
    }
}