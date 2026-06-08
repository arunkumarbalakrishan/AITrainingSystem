using AITrainingSystem.Application.DTOs.LessonNote;
using AITrainingSystem.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AITrainingSystem.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class LessonNoteController : ControllerBase
{
    private readonly ILessonNoteService _noteService;

    public LessonNoteController(ILessonNoteService noteService)
    {
        _noteService = noteService;
    }

    [HttpPost]
    public async Task<IActionResult> CreateNote([FromBody] CreateLessonNoteDto dto)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _noteService.CreateNoteAsync(userId, dto);
        if (!result.Success)
            return BadRequest(result);
        return Ok(result);
    }

    [HttpGet("lesson/{lessonId}")]
    public async Task<IActionResult> GetNotesByLesson(Guid lessonId)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _noteService.GetNotesByLessonAsync(userId, lessonId);
        return Ok(result);
    }

    [HttpPut("{noteId}")]
    public async Task<IActionResult> UpdateNote(Guid noteId, [FromBody] UpdateLessonNoteDto dto)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _noteService.UpdateNoteAsync(userId, noteId, dto);
        if (!result.Success)
            return BadRequest(result);
        return Ok(result);
    }

    [HttpDelete("{noteId}")]
    public async Task<IActionResult> DeleteNote(Guid noteId)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _noteService.DeleteNoteAsync(userId, noteId);
        if (!result.Success)
            return BadRequest(result);
        return Ok(result);
    }
}
