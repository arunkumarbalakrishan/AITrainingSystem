using AITrainingSystem.Application.DTOs.LiveClasses;
using AITrainingSystem.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace AITrainingSystem.API.Controllers
{
    [ApiController]
    [Route("api/live-class-sessions")]
    [Authorize]
    public class LiveClassSessionController : ControllerBase
    {
        private readonly ILiveClassSessionService _sessionService;

        public LiveClassSessionController(ILiveClassSessionService sessionService)
        {
            _sessionService = sessionService;
        }

        [HttpGet("{liveClassId}/attendance")]
        [Authorize(Roles = "Trainer,Admin")]
        public async Task<IActionResult> GetAttendance(Guid liveClassId)
        {
            var result = await _sessionService.GetAttendanceAsync(liveClassId);
            if (!result.Success)
            {
                return BadRequest(result);
            }
            return Ok(result);
        }

        [HttpPost("{liveClassId}/attendance/join")]
        public async Task<IActionResult> RecordJoin(Guid liveClassId, [FromQuery] string userName)
        {
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var result = await _sessionService.RecordJoinTimeAsync(liveClassId, userId, userName);
            if (!result.Success)
            {
                return BadRequest(result);
            }
            return Ok(result);
        }

        [HttpPost("{liveClassId}/attendance/leave")]
        public async Task<IActionResult> RecordLeave(Guid liveClassId)
        {
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var result = await _sessionService.RecordLeaveTimeAsync(liveClassId, userId);
            if (!result.Success)
            {
                return BadRequest(result);
            }
            return Ok(result);
        }

        [HttpGet("{liveClassId}/recordings")]
        public async Task<IActionResult> GetRecordings(Guid liveClassId)
        {
            var result = await _sessionService.GetRecordingsAsync(liveClassId);
            if (!result.Success)
            {
                return BadRequest(result);
            }
            return Ok(result);
        }

        [HttpPost("{liveClassId}/recordings")]
        [Authorize(Roles = "Trainer,Admin")]
        public async Task<IActionResult> AddRecording(Guid liveClassId, [FromBody] AddRecordingDto dto)
        {
            var result = await _sessionService.AddRecordingAsync(liveClassId, dto);
            if (!result.Success)
            {
                return BadRequest(result);
            }
            return Ok(result);
        }

        [HttpPost("{liveClassId}/recordings/upload")]
        [Authorize(Roles = "Trainer,Admin")]
        public async Task<IActionResult> UploadRecording(Guid liveClassId, IFormFile file, [FromQuery] double durationInMinutes)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("Invalid recording file.");
            }

            using var stream = file.OpenReadStream();
            var result = await _sessionService.UploadRecordingFileAsync(liveClassId, stream, file.FileName, durationInMinutes);
            if (!result.Success)
            {
                return BadRequest(result);
            }
            return Ok(result);
        }
    }
}
