using AITrainingSystem.Application.Common.Models;
using AITrainingSystem.Application.DTOs.LiveClasses;
using AITrainingSystem.Application.Interfaces.Services;
using AITrainingSystem.Domain.Entities;
using AITrainingSystem.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace AITrainingSystem.Infrastructure.Services.LiveClasses
{
    public class LiveClassSessionService : ILiveClassSessionService
    {
        private readonly ApplicationDbContext _context;
        private readonly IStorageService _storageService;

        public LiveClassSessionService(ApplicationDbContext context, IStorageService storageService)
        {
            _context = context;
            _storageService = storageService;
        }

        public async Task<ApiResponse<IEnumerable<LiveClassAttendanceDto>>> GetAttendanceAsync(Guid liveClassId)
        {
            try
            {
                var attendance = await _context.LiveClassAttendances
                    .Where(a => a.LiveClassId == liveClassId)
                    .OrderBy(a => a.JoinTime)
                    .Select(a => MapAttendanceDto(a))
                    .ToListAsync();

                return ApiResponse<IEnumerable<LiveClassAttendanceDto>>.SuccessResponse(attendance, "Attendance log retrieved successfully.");
            }
            catch (Exception ex)
            {
                return ApiResponse<IEnumerable<LiveClassAttendanceDto>>.FailResponse($"Failed to retrieve attendance: {ex.Message}");
            }
        }

        public async Task<ApiResponse<LiveClassAttendanceDto>> RecordJoinTimeAsync(Guid liveClassId, Guid userId, string userName)
        {
            try
            {
                // Double check if there's already an active connection logic session
                var activeSession = await _context.LiveClassAttendances
                    .Where(a => a.LiveClassId == liveClassId && a.UserId == userId && a.LeaveTime == null)
                    .FirstOrDefaultAsync();

                if (activeSession != null)
                {
                    return ApiResponse<LiveClassAttendanceDto>.SuccessResponse(MapAttendanceDto(activeSession), "User is already logged as active in this session.");
                }

                var attendance = new LiveClassAttendance
                {
                    Id = Guid.NewGuid(),
                    LiveClassId = liveClassId,
                    UserId = userId,
                    UserName = userName,
                    JoinTime = DateTime.UtcNow
                };

                _context.LiveClassAttendances.Add(attendance);
                await _context.SaveChangesAsync();

                return ApiResponse<LiveClassAttendanceDto>.SuccessResponse(MapAttendanceDto(attendance), "User join logged successfully.");
            }
            catch (Exception ex)
            {
                return ApiResponse<LiveClassAttendanceDto>.FailResponse($"Failed to record join time: {ex.Message}");
            }
        }

        public async Task<ApiResponse<LiveClassAttendanceDto>> RecordLeaveTimeAsync(Guid liveClassId, Guid userId)
        {
            try
            {
                var activeSession = await _context.LiveClassAttendances
                    .Where(a => a.LiveClassId == liveClassId && a.UserId == userId && a.LeaveTime == null)
                    .OrderByDescending(a => a.JoinTime)
                    .FirstOrDefaultAsync();

                if (activeSession == null)
                {
                    return ApiResponse<LiveClassAttendanceDto>.FailResponse("No active session found for this user in the specified live class.");
                }

                activeSession.LeaveTime = DateTime.UtcNow;
                activeSession.DurationInMinutes = (activeSession.LeaveTime.Value - activeSession.JoinTime).TotalMinutes;

                _context.LiveClassAttendances.Update(activeSession);
                await _context.SaveChangesAsync();

                return ApiResponse<LiveClassAttendanceDto>.SuccessResponse(MapAttendanceDto(activeSession), "User leave logged successfully.");
            }
            catch (Exception ex)
            {
                return ApiResponse<LiveClassAttendanceDto>.FailResponse($"Failed to record leave time: {ex.Message}");
            }
        }

        public async Task<ApiResponse<LiveClassRecordingDto>> AddRecordingAsync(Guid liveClassId, AddRecordingDto dto)
        {
            try
            {
                var liveClassExists = await _context.LiveClasses.AnyAsync(l => l.Id == liveClassId);
                if (!liveClassExists)
                {
                    return ApiResponse<LiveClassRecordingDto>.FailResponse("Live class not found.");
                }

                var recording = new LiveClassRecording
                {
                    Id = Guid.NewGuid(),
                    LiveClassId = liveClassId,
                    DownloadUrl = dto.DownloadUrl,
                    FilePath = dto.DownloadUrl,
                    FileSizeInBytes = dto.FileSizeInBytes,
                    DurationInMinutes = dto.DurationInMinutes,
                    CreatedAt = DateTime.UtcNow
                };

                _context.LiveClassRecordings.Add(recording);
                await _context.SaveChangesAsync();

                return ApiResponse<LiveClassRecordingDto>.SuccessResponse(MapRecordingToDto(recording), "Recording linked successfully.");
            }
            catch (Exception ex)
            {
                return ApiResponse<LiveClassRecordingDto>.FailResponse($"Failed to add recording: {ex.Message}");
            }
        }

        public async Task<ApiResponse<LiveClassRecordingDto>> UploadRecordingFileAsync(Guid liveClassId, Stream fileStream, string fileName, double durationInMinutes)
        {
            try
            {
                var liveClassExists = await _context.LiveClasses.AnyAsync(l => l.Id == liveClassId);
                if (!liveClassExists)
                {
                    return ApiResponse<LiveClassRecordingDto>.FailResponse("Live class not found.");
                }

                var folderName = $"recordings/{liveClassId}";
                var uploadedUrl = await _storageService.UploadFileAsync(fileStream, fileName, folderName, "video/mp4");
                var fileSize = fileStream.CanSeek ? fileStream.Length : 0;

                var recording = new LiveClassRecording
                {
                    Id = Guid.NewGuid(),
                    LiveClassId = liveClassId,
                    DownloadUrl = uploadedUrl,
                    FilePath = $"{folderName}/{fileName}",
                    FileSizeInBytes = fileSize,
                    DurationInMinutes = durationInMinutes,
                    CreatedAt = DateTime.UtcNow
                };

                _context.LiveClassRecordings.Add(recording);
                await _context.SaveChangesAsync();

                return ApiResponse<LiveClassRecordingDto>.SuccessResponse(MapRecordingToDto(recording), "Recording file uploaded successfully.");
            }
            catch (Exception ex)
            {
                return ApiResponse<LiveClassRecordingDto>.FailResponse($"Failed to upload recording file: {ex.Message}");
            }
        }

        public async Task<ApiResponse<IEnumerable<LiveClassRecordingDto>>> GetRecordingsAsync(Guid liveClassId)
        {
            try
            {
                var recordings = await _context.LiveClassRecordings
                    .Where(r => r.LiveClassId == liveClassId)
                    .OrderByDescending(r => r.CreatedAt)
                    .Select(r => MapRecordingToDto(r))
                    .ToListAsync();

                return ApiResponse<IEnumerable<LiveClassRecordingDto>>.SuccessResponse(recordings, "Recordings list retrieved successfully.");
            }
            catch (Exception ex)
            {
                return ApiResponse<IEnumerable<LiveClassRecordingDto>>.FailResponse($"Failed to retrieve recordings: {ex.Message}");
            }
        }

        private static LiveClassAttendanceDto MapAttendanceDto(LiveClassAttendance attendance)
        {
            return new LiveClassAttendanceDto
            {
                Id = attendance.Id,
                LiveClassId = attendance.LiveClassId,
                UserId = attendance.UserId,
                UserName = attendance.UserName,
                JoinTime = attendance.JoinTime,
                LeaveTime = attendance.LeaveTime,
                DurationInMinutes = attendance.DurationInMinutes
            };
        }

        private static LiveClassRecordingDto MapRecordingToDto(LiveClassRecording recording)
        {
            return new LiveClassRecordingDto
            {
                Id = recording.Id,
                LiveClassId = recording.LiveClassId,
                FilePath = recording.FilePath,
                DownloadUrl = recording.DownloadUrl,
                FileSizeInBytes = recording.FileSizeInBytes,
                DurationInMinutes = recording.DurationInMinutes,
                CreatedAt = recording.CreatedAt
            };
        }
    }
}
