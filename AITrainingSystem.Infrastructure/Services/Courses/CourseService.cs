using AITrainingSystem.Application.Common.Models;
using AITrainingSystem.Application.DTOs.Course;
using AITrainingSystem.Application.DTOs.Lesson;
using AITrainingSystem.Application.Interfaces.Repositories;
using AITrainingSystem.Application.Interfaces.Services;
using AITrainingSystem.Domain.Entities;

namespace AITrainingSystem.Infrastructure.Services.Courses;

public class CourseService : ICourseService
{
    private readonly ICourseRepository _courseRepository;
    

    public CourseService(ICourseRepository courseRepository)
    {
        _courseRepository = courseRepository;
    }

    public async Task<ApiResponse<CourseResponseDto>> CreateAsync(CreateCourseDto dto)
    {
        var course = new Course
        {
            Id = Guid.NewGuid(),
            Title = dto.Title,
            Description = dto.Description,
            Price = dto.Price,
            DurationInHours = dto.DurationInHours,
            ThumbnailUrl = dto.ThumbnailUrl,
            CreatedAt = DateTime.UtcNow
        };

        var createdCourse = await _courseRepository.CreateAsync(course);

        var response = new CourseResponseDto
        {
            Id = createdCourse.Id,
            Title = createdCourse.Title,
            Description = createdCourse.Description,
            Price = createdCourse.Price,
            DurationInHours = createdCourse.DurationInHours,
            ThumbnailUrl = createdCourse.ThumbnailUrl,
            IsPublished = createdCourse.IsPublished,
            CreatedAt = createdCourse.CreatedAt
        };

        return ApiResponse<CourseResponseDto>.SuccessResponse(
            response,
            "Course created successfully"
        );
    }

    public async Task<ApiResponse<IEnumerable<CourseResponseDto>>> GetAllAsync()
    {
        var courses = await _courseRepository.GetAllAsync();

        var response = courses.Select(x => new CourseResponseDto
        {
            Id = x.Id,
            Title = x.Title,
            Description = x.Description,
            Price = x.Price,
            DurationInHours = x.DurationInHours,
            ThumbnailUrl = x.ThumbnailUrl,
            IsPublished = x.IsPublished,
            CreatedAt = x.CreatedAt
        });

        return ApiResponse<IEnumerable<CourseResponseDto>>.SuccessResponse(
            response,
            "Courses fetched successfully"
        );
    }

    public async Task<ApiResponse<CourseResponseDto>> GetByIdAsync(Guid id)
    {
        var course = await _courseRepository.GetByIdAsync(id);

        if (course is null)
        {
            return ApiResponse<CourseResponseDto>.FailResponse("Course not found");
        }

        var response = new CourseResponseDto
        {
            Id = course.Id,
            Title = course.Title,
            Description = course.Description,
            Price = course.Price,
            DurationInHours = course.DurationInHours,
            ThumbnailUrl = course.ThumbnailUrl,
            IsPublished = course.IsPublished,
            CreatedAt = course.CreatedAt
        };

        return ApiResponse<CourseResponseDto>.SuccessResponse(
            response,
            "Course fetched successfully"
        );
    }

    public async Task<ApiResponse<CourseResponseDto>> UpdateAsync(Guid id, UpdateCourseDto dto)
    {
        var course = await _courseRepository.GetByIdAsync(id);

        if (course is null)
        {
            return ApiResponse<CourseResponseDto>.FailResponse("Course not found");
        }

        course.Title = dto.Title;
        course.Description = dto.Description;
        course.Price = dto.Price;
        course.DurationInHours = dto.DurationInHours;
        course.ThumbnailUrl = dto.ThumbnailUrl;
        course.IsPublished = dto.IsPublished;
        course.UpdatedAt = DateTime.UtcNow;

        await _courseRepository.UpdateAsync(course);

        var response = new CourseResponseDto
        {
            Id = course.Id,
            Title = course.Title,
            Description = course.Description,
            Price = course.Price,
            DurationInHours = course.DurationInHours,
            ThumbnailUrl = course.ThumbnailUrl,
            IsPublished = course.IsPublished,
            CreatedAt = course.CreatedAt
        };

        return ApiResponse<CourseResponseDto>.SuccessResponse(
            response,
            "Course updated successfully"
        );
    }

    public async Task<ApiResponse<string>> DeleteAsync(Guid id)
    {
        var course = await _courseRepository.GetByIdAsync(id);

        if (course is null)
        {
            return ApiResponse<string>.FailResponse("Course not found");
        }

        await _courseRepository.DeleteAsync(course);

        return ApiResponse<string>.SuccessResponse(
            "Course deleted successfully"
        );
    }
    //public async Task<ApiResponse<CourseWithLessonsDto>> GetCourseWithLessonsAsync(Guid courseId)
    //{
    //    var course = await _courseRepository.GetByIdWithLessonsAsync(courseId);

    //    if (course == null)
    //        return ApiResponse<CourseWithLessonsDto>.FailResponse("Course not found");

    //    return ApiResponse<CourseWithLessonsDto>.SuccessResponse(
    //        new CourseWithLessonsDto
    //        {
    //            Id = course.Id,
    //            Title = course.Title,
    //            Description = course.Description,

    //            Lessons = course.Lessons
    //            .OrderBy(x => x.Order)
    //            .Select(l => new LessonResponseDto
    //            {
    //                Id = l.Id,
    //                CourseId = l.CourseId,
    //                Title = l.Title,
    //                Description = l.Description,
    //                VideoUrl = l.VideoUrl,
    //                PdfUrl = l.PdfUrl,
    //                DurationInMinutes = l.DurationInMinutes,
    //                Order = l.Order,
    //                IsPreviewFree = l.IsPreviewFree
    //            }).ToList()
    //        } );
    //}
    public async Task<CourseFullDto?> GetCourseFullAsync(Guid courseId, Guid userId)
    {
        return await _courseRepository.GetCourseFullOptimizedAsync(courseId, userId);
    }
}