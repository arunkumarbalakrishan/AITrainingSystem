using AITrainingSystem.Application.DTOs.Course;
using AITrainingSystem.Application.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace AITrainingSystem.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CourseController : ControllerBase
{
    private readonly ICourseService _courseService;

    public CourseController(ICourseService courseService)
    {
        _courseService = courseService;
    }

    // CREATE COURSE
    [HttpPost]
    public async Task<IActionResult> CreateCourse(CreateCourseDto dto)
    {
        var response = await _courseService.CreateAsync(dto);

        return Ok(response);
    }

    // GET ALL COURSES
    [HttpGet]
    public async Task<IActionResult> GetAllCourses()
    {
        var response = await _courseService.GetAllAsync();

        return Ok(response);
    }

    // GET COURSE BY ID
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

    // UPDATE COURSE
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

    // DELETE COURSE
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCourse(Guid id)
    {
        var response = await _courseService.DeleteAsync(id);

        if (!response.Success)
        {
            return NotFound(response);
        }

        return Ok(response);
    }
}