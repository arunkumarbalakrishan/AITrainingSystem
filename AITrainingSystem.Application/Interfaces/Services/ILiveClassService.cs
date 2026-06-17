using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AITrainingSystem.Application.Common.Models;
using AITrainingSystem.Application.DTOs.LiveClasses;

namespace AITrainingSystem.Application.Interfaces.Services
{
    public interface ILiveClassService
    {
        Task<ApiResponse<LiveClassDto>> CreateLiveClassAsync(CreateLiveClassDto dto, Guid trainerId);
        Task<ApiResponse<IEnumerable<LiveClassDto>>> GetUpcomingLiveClassesAsync();
        Task<ApiResponse<IEnumerable<LiveClassDto>>> GetUpcomingLiveClassesByCourseAsync(Guid courseId);
        Task<ApiResponse<bool>> DeleteLiveClassAsync(Guid id, Guid trainerId, string userRole);
    }
}
