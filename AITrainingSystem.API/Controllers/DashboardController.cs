using AITrainingSystem.Application.Interfaces.DashboardService;
using AITrainingSystem.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AITrainingSystem.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;

    public DashboardController(
        IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    [HttpGet("analytics")]
    public async Task<IActionResult> GetAnalytics()
    {
        var userId = Guid.Parse(
            User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        var result =
            await _dashboardService
                .GetAnalyticsAsync(userId);

        return Ok(result);
    }

    [HttpGet("recently-completed")]
    public async Task<IActionResult> GetRecentlyCompletedCourses()
    {
        var userId = Guid.Parse(
            User.FindFirst(
                ClaimTypes.NameIdentifier)!.Value);

        var result =
            await _dashboardService
                .GetRecentlyCompletedCoursesAsync(userId);

        return Ok(result);
    }
}