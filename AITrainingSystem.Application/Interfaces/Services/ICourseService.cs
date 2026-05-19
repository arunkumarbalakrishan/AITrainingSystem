using AITrainingSystem.Application.Common.Models;
using AITrainingSystem.Application.DTOs.Course;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AITrainingSystem.Application.Interfaces.Services;

public interface ICourseService
{
    Task<ApiResponse<CourseResponseDto>> CreateAsync(CreateCourseDto dto);

    Task<ApiResponse<IEnumerable<CourseResponseDto>>> GetAllAsync();

    Task<ApiResponse<CourseResponseDto>> GetByIdAsync(Guid id);

    Task<ApiResponse<CourseResponseDto>> UpdateAsync(Guid id, UpdateCourseDto dto);

    Task<ApiResponse<string>> DeleteAsync(Guid id);
}
