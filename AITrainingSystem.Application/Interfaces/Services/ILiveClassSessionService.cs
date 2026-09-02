using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using AITrainingSystem.Application.Common.Models;
using AITrainingSystem.Application.DTOs.LiveClasses;

namespace AITrainingSystem.Application.Interfaces.Services
{
    public interface ILiveClassSessionService
    {
        Task<ApiResponse<IEnumerable<LiveClassAttendanceDto>>> GetAttendanceAsync(Guid liveClassId);
        Task<ApiResponse<LiveClassAttendanceDto>> RecordJoinTimeAsync(Guid liveClassId, Guid userId, string userName);
        Task<ApiResponse<LiveClassAttendanceDto>> RecordLeaveTimeAsync(Guid liveClassId, Guid userId);
        
        Task<ApiResponse<LiveClassRecordingDto>> AddRecordingAsync(Guid liveClassId, AddRecordingDto dto);
        Task<ApiResponse<LiveClassRecordingDto>> UploadRecordingFileAsync(Guid liveClassId, Stream fileStream, string fileName, double durationInMinutes);
        Task<ApiResponse<IEnumerable<LiveClassRecordingDto>>> GetRecordingsAsync(Guid liveClassId);
    }
}
