using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AITrainingSystem.Application.Common.Models;
using AITrainingSystem.Application.DTOs.LiveClasses;
using AITrainingSystem.Application.Interfaces.Services;
using AITrainingSystem.Domain.Entities;
using AITrainingSystem.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace AITrainingSystem.Infrastructure.Services.LiveClasses
{
    public class LiveClassService : ILiveClassService
    {
        private readonly ApplicationDbContext _context;
        private readonly IZoomApiService _zoomApiService;
        private readonly IJitsiMeetingService _jitsiMeetingService;

        public LiveClassService(
            ApplicationDbContext context,
            IZoomApiService zoomApiService,
            IJitsiMeetingService jitsiMeetingService)
        {
            _context = context;
            _zoomApiService = zoomApiService;
            _jitsiMeetingService = jitsiMeetingService;
        }

        public async Task<ApiResponse<LiveClassDto>> CreateLiveClassAsync(CreateLiveClassDto dto, Guid trainerId)
        {
            try
            {
                var classId = Guid.NewGuid();
                string meetingLink = dto.MeetingLink ?? string.Empty;
                string provider = string.IsNullOrWhiteSpace(dto.Provider) ? "Auto" : dto.Provider;
                string? passcode = null;
                string? hostStartUrl = null;

                // 1. Explicit Custom Link
                if (provider.Equals("Custom", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(meetingLink))
                {
                    provider = "Custom";
                }
                // 2. Zoom API Meeting Request
                else if (provider.Equals("Zoom", StringComparison.OrdinalIgnoreCase))
                {
                    var zoomRes = await _zoomApiService.CreateMeetingAsync(dto.Title, dto.Description, dto.StartTime, dto.DurationInMinutes);
                    if (zoomRes.Success)
                    {
                        meetingLink = zoomRes.MeetingLink;
                        hostStartUrl = zoomRes.HostStartUrl;
                        passcode = zoomRes.Passcode;
                        provider = "Zoom";
                    }
                    else
                    {
                        // Fallback to Jitsi if Zoom configuration/API fails
                        meetingLink = _jitsiMeetingService.GenerateMeetingLink(dto.Title, classId);
                        provider = "Jitsi";
                    }
                }
                // 3. Jitsi WebRTC (Default out-of-the-box in-app streaming)
                else
                {
                    meetingLink = _jitsiMeetingService.GenerateMeetingLink(dto.Title, classId);
                    provider = "Jitsi";
                }

                var liveClass = new LiveClass
                {
                    Id = classId,
                    Title = dto.Title,
                    Description = dto.Description,
                    MeetingLink = meetingLink,
                    Provider = provider,
                    Passcode = passcode,
                    HostStartUrl = hostStartUrl,
                    StartTime = dto.StartTime,
                    DurationInMinutes = dto.DurationInMinutes,
                    CourseId = dto.CourseId,
                    TrainerId = trainerId,
                    CreatedAt = DateTime.UtcNow
                };

                _context.LiveClasses.Add(liveClass);
                await _context.SaveChangesAsync();

                // Reload to include Course and Trainer info
                var savedClass = await _context.LiveClasses
                    .Include(l => l.Course)
                    .Include(l => l.Trainer)
                    .FirstOrDefaultAsync(l => l.Id == liveClass.Id);

                if (savedClass == null)
                {
                    return ApiResponse<LiveClassDto>.FailResponse("Failed to save and retrieve live class.");
                }

                var resultDto = MapToDto(savedClass);
                return ApiResponse<LiveClassDto>.SuccessResponse(resultDto, "Live class scheduled successfully.");
            }
            catch (Exception ex)
            {
                return ApiResponse<LiveClassDto>.FailResponse($"Failed to create live class: {ex.Message}");
            }
        }

        public async Task<ApiResponse<IEnumerable<LiveClassDto>>> GetUpcomingLiveClassesAsync()
        {
            try
            {
                var now = DateTime.UtcNow;
                var classes = await _context.LiveClasses
                    .Include(l => l.Course)
                    .Include(l => l.Trainer)
                    .Where(l => l.StartTime.AddMinutes(l.DurationInMinutes) > now)
                    .OrderBy(l => l.StartTime)
                    .ToListAsync();

                var dtos = classes.Select(MapToDto);
                return ApiResponse<IEnumerable<LiveClassDto>>.SuccessResponse(dtos, "Upcoming live classes retrieved.");
            }
            catch (Exception ex)
            {
                return ApiResponse<IEnumerable<LiveClassDto>>.FailResponse($"Failed to retrieve live classes: {ex.Message}");
            }
        }

        public async Task<ApiResponse<IEnumerable<LiveClassDto>>> GetUpcomingLiveClassesByCourseAsync(Guid courseId)
        {
            try
            {
                var now = DateTime.UtcNow;
                var classes = await _context.LiveClasses
                    .Include(l => l.Course)
                    .Include(l => l.Trainer)
                    .Where(l => l.CourseId == courseId && l.StartTime.AddMinutes(l.DurationInMinutes) > now)
                    .OrderBy(l => l.StartTime)
                    .ToListAsync();

                var dtos = classes.Select(MapToDto);
                return ApiResponse<IEnumerable<LiveClassDto>>.SuccessResponse(dtos, "Upcoming live classes for course retrieved.");
            }
            catch (Exception ex)
            {
                return ApiResponse<IEnumerable<LiveClassDto>>.FailResponse($"Failed to retrieve live classes for course: {ex.Message}");
            }
        }

        public async Task<ApiResponse<bool>> DeleteLiveClassAsync(Guid id, Guid trainerId, string userRole)
        {
            try
            {
                var liveClass = await _context.LiveClasses.FindAsync(id);
                if (liveClass == null)
                {
                    return ApiResponse<bool>.FailResponse("Live class not found.");
                }

                // Check authorization (only scheduled trainer or admin can delete)
                if (userRole != "Admin" && liveClass.TrainerId != trainerId)
                {
                    return ApiResponse<bool>.FailResponse("Unauthorized to delete this live class.");
                }

                _context.LiveClasses.Remove(liveClass);
                await _context.SaveChangesAsync();

                return ApiResponse<bool>.SuccessResponse(true, "Live class deleted successfully.");
            }
            catch (Exception ex)
            {
                return ApiResponse<bool>.FailResponse($"Failed to delete live class: {ex.Message}");
            }
        }

        private static LiveClassDto MapToDto(LiveClass liveClass)
        {
            return new LiveClassDto
            {
                Id = liveClass.Id,
                Title = liveClass.Title,
                Description = liveClass.Description,
                MeetingLink = liveClass.MeetingLink,
                Provider = liveClass.Provider ?? "Jitsi",
                Passcode = liveClass.Passcode,
                HostStartUrl = liveClass.HostStartUrl,
                StartTime = liveClass.StartTime,
                DurationInMinutes = liveClass.DurationInMinutes,
                CourseId = liveClass.CourseId,
                CourseTitle = liveClass.Course?.Title,
                TrainerId = liveClass.TrainerId,
                TrainerName = liveClass.Trainer?.FullName ?? "Unknown Trainer",
                CreatedAt = liveClass.CreatedAt
            };
        }
    }
}
