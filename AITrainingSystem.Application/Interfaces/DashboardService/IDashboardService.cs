using AITrainingSystem.Application.DTOs.Analytics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AITrainingSystem.Application.Interfaces.DashboardService
{
    public interface IDashboardService
    {
        Task<LearningAnalyticsDto> GetAnalyticsAsync(Guid userId);
        Task<List<RecentlyCompletedCourseDto>> GetRecentlyCompletedCoursesAsync(Guid userId);
    }
}
