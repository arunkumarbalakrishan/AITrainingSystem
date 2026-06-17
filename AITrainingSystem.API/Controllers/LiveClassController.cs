using AITrainingSystem.Application.DTOs.LiveClasses;
using AITrainingSystem.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace AITrainingSystem.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class LiveClassController : ControllerBase
    {
        private readonly ILiveClassService _liveClassService;

        public LiveClassController(ILiveClassService liveClassService)
        {
            _liveClassService = liveClassService;
        }

        [HttpPost]
        [Authorize(Roles = "Trainer,Admin")]
        public async Task<IActionResult> CreateLiveClass([FromBody] CreateLiveClassDto dto)
        {
            var trainerId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var result = await _liveClassService.CreateLiveClassAsync(dto, trainerId);
            
            if (!result.Success)
            {
                return BadRequest(result);
            }
            return Ok(result);
        }

        [HttpGet("upcoming")]
        public async Task<IActionResult> GetUpcomingLiveClasses()
        {
            var result = await _liveClassService.GetUpcomingLiveClassesAsync();
            if (!result.Success)
            {
                return BadRequest(result);
            }
            return Ok(result);
        }

        [HttpGet("course/{courseId}")]
        public async Task<IActionResult> GetUpcomingLiveClassesByCourse(Guid courseId)
        {
            var result = await _liveClassService.GetUpcomingLiveClassesByCourseAsync(courseId);
            if (!result.Success)
            {
                return BadRequest(result);
            }
            return Ok(result);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Trainer,Admin")]
        public async Task<IActionResult> DeleteLiveClass(Guid id)
        {
            var trainerId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var userRole = User.FindFirst(ClaimTypes.Role)!.Value;
            var result = await _liveClassService.DeleteLiveClassAsync(id, trainerId, userRole);
            
            if (!result.Success)
            {
                return BadRequest(result);
            }
            return Ok(result);
        }
    }
}
