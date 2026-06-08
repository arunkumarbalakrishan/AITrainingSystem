using AITrainingSystem.Application.DTOs.Quiz;
using AITrainingSystem.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AITrainingSystem.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class QuizController : ControllerBase
{
    private readonly IAssessmentService _assessmentService;

    public QuizController(IAssessmentService assessmentService)
    {
        _assessmentService = assessmentService;
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Trainer")]
    public async Task<IActionResult> CreateQuiz([FromBody] CreateQuizDto dto)
    {
        var result = await _assessmentService.CreateQuizAsync(dto);
        if (!result.Success)
            return BadRequest(result);
        return Ok(result);
    }

    [HttpGet("{quizId}")]
    [Authorize]
    public async Task<IActionResult> GetQuiz(Guid quizId)
    {
        var result = await _assessmentService.GetQuizByIdAsync(quizId);
        if (!result.Success)
            return NotFound(result);
        return Ok(result);
    }

    [HttpGet("course/{courseId}")]
    [Authorize]
    public async Task<IActionResult> GetQuizzesByCourse(Guid courseId)
    {
        var result = await _assessmentService.GetQuizzesByCourseIdAsync(courseId);
        return Ok(result);
    }

    [HttpPost("submit")]
    [Authorize]
    public async Task<IActionResult> SubmitQuiz([FromBody] QuizSubmitDto dto)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _assessmentService.SubmitQuizAsync(userId, dto);
        if (!result.Success)
            return BadRequest(result);
        return Ok(result);
    }

    [HttpGet("results")]
    [Authorize]
    public async Task<IActionResult> GetUserResults()
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _assessmentService.GetUserResultsAsync(userId);
        return Ok(result);
    }
}
