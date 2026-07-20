using AITrainingSystem.Application.DTOs.AI;
using AITrainingSystem.Application.DTOs.Quiz;
using AITrainingSystem.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AITrainingSystem.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AIController : ControllerBase
{
    private readonly IAIService _aiService;

    public AIController(IAIService aiService)
    {
        _aiService = aiService;
    }

    [HttpPost("tutor/{courseId}")]
    public async Task<IActionResult> AskTutor(Guid courseId, [FromBody] TutorRequestDto request)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _aiService.AskTutorAsync(userId, courseId, request);
        if (!result.Success)
            return BadRequest(result);
        return Ok(result);
    }

    [HttpPost("generate-quiz")]
    public async Task<IActionResult> GenerateQuiz([FromBody] QuizGenRequestDto request)
    {
        var result = await _aiService.GenerateQuizAsync(request);
        if (!result.Success)
            return BadRequest(result);
        return Ok(result);
    }

    [HttpGet("recommendations")]
    public async Task<IActionResult> GetRecommendations()
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _aiService.GetRecommendationsAsync(userId);
        return Ok(result);
    }

    [HttpPost("mock-interview")]
    public async Task<IActionResult> MockInterviewStep([FromBody] MockInterviewStepDto request)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _aiService.ConductMockInterviewStepAsync(userId, request);
        if (!result.Success)
            return BadRequest(result);
        return Ok(result);
    }

    [HttpPost("mock-interview/start")]
    public async Task<IActionResult> StartMockInterview([FromBody] StartMockInterviewDto request)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _aiService.StartMockInterviewAsync(userId, request);
        if (!result.Success)
            return BadRequest(result);
        return Ok(result);
    }

    [HttpPost("mock-interview/step")]
    public async Task<IActionResult> SubmitMockInterviewStep([FromBody] SubmitMockInterviewStepDto request)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _aiService.SubmitMockInterviewStepAsync(userId, request);
        if (!result.Success)
            return BadRequest(result);
        return Ok(result);
    }

    [HttpGet("mock-interview/history")]
    public async Task<IActionResult> GetMockInterviewHistory()
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _aiService.GetMockInterviewHistoryAsync(userId);
        if (!result.Success)
            return BadRequest(result);
        return Ok(result);
    }

    [HttpGet("mock-interview/{id}/scorecard")]
    public async Task<IActionResult> GetMockInterviewScorecard(Guid id)
    {
        var role = User.FindFirstValue(ClaimTypes.Role);
        var userId = role == "Admin" ? Guid.Empty : Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _aiService.GetMockInterviewScorecardAsync(userId, id);
        if (!result.Success)
            return BadRequest(result);
        return Ok(result);
    }

    [HttpPost("mock-interview/{id}/video")]
    public async Task<IActionResult> UploadInterviewVideo(Guid id, IFormFile file)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _aiService.UploadInterviewVideoAsync(userId, id, file);
        if (!result.Success)
            return BadRequest(result);
        return Ok(result);
    }

    [HttpPost("resume-analysis")]
    public async Task<IActionResult> AnalyzeResume([FromBody] ResumeAnalysisRequestDto request)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _aiService.AnalyzeResumeAsync(userId, request);
        if (!result.Success)
            return BadRequest(result);
        return Ok(result);
    }

    [HttpGet("admin/mock-interviews")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAllMockInterviews()
    {
        var result = await _aiService.GetAllMockInterviewsAsync();
        if (!result.Success)
            return BadRequest(result);
        return Ok(result);
    }
}
