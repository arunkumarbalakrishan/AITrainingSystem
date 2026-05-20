using AITrainingSystem.Application.DTOs.Lesson;
using Microsoft.AspNetCore.Mvc;

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
        return Ok(id);
    }

    [HttpGet("course/{courseId}")]
    public async Task<IActionResult> GetByCourse(Guid courseId)
    {
        var result = await _service.GetByCourseIdAsync(courseId);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _service.GetByIdAsync(id);
        return Ok(result);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, UpdateLessonDto dto)
    {
        var result = await _service.UpdateAsync(id, dto);
        return Ok(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _service.DeleteAsync(id);
        return Ok(result);
    }
}