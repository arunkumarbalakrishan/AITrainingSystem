using System;
using System.Threading.Tasks;
using AITrainingSystem.Application.DTOs.LiveClasses;

namespace AITrainingSystem.Application.Interfaces.Services
{
    public interface IZoomApiService
    {
        Task<ZoomMeetingResponseDto> CreateMeetingAsync(string topic, string description, DateTime startTime, int durationMinutes);
    }

    public interface IJitsiMeetingService
    {
        string GenerateMeetingLink(string topic, Guid classId);
    }
}
