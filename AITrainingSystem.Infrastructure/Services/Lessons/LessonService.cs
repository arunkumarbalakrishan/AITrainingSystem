using AITrainingSystem.Application.DTOs.Lesson;
using AITrainingSystem.Application.Features.Media.Interfaces;
using AITrainingSystem.Application.Interfaces.Lessons;
using AITrainingSystem.Application.Interfaces.Repositories;
using AITrainingSystem.Domain.Entities;
using Microsoft.Extensions.Logging;

public class LessonService : ILessonService
{
    private readonly ILessonRepository _repo;
    private readonly ICourseRepository _courseRepo;
    private readonly IEnrollmentRepository _enrollmentRepo;
    private readonly IMediaAccessService _mediaService;

    public LessonService(ILessonRepository repo, ICourseRepository courseRepo, IEnrollmentRepository enrollmentRepo, IMediaAccessService mediaService)
    {
        _repo = repo;
        _courseRepo = courseRepo;
        _enrollmentRepo = enrollmentRepo;
        _mediaService = mediaService;
    }

    public async Task<Guid> CreateAsync(CreateLessonDto dto)
    {
        var course = await _courseRepo.GetByIdAsync(dto.CourseId);

        if (course == null)
            throw new Exception("Course not found");

        var nextOrder = await _repo.GetNextOrderAsync(dto.CourseId);

        
        if (await _repo.OrderExistsAsync(dto.CourseId, nextOrder))
        {
            throw new Exception("Lesson order conflict detected");
        }

        var lesson = new Lesson
        {
            Id = Guid.NewGuid(),
            CourseId = dto.CourseId,
            Title = dto.Title,
            Description = dto.Description,
            VideoKey = dto.VideoUrl,
            PdfKey = dto.PdfUrl,
            DurationInMinutes = dto.DurationInMinutes,
            Order = nextOrder,
            IsPreviewFree = dto.IsPreviewFree
        };

        await _repo.AddAsync(lesson);

        return lesson.Id;
    }

    public async Task<List<LessonResponseDto>> GetByCourseIdAsync(Guid courseId)
    {
        var lessons = await _repo.GetByCourseIdAsync(courseId);

        return lessons.Select(x => new LessonResponseDto
        {
            Id = x.Id,
            CourseId = x.CourseId,
            Title = x.Title,
            Description = x.Description,

            // SECURE MEDIA ENDPOINTS
            VideoUrl = $"/api/media/video/{x.Id}",

            PdfUrl = x.PdfKey == null
                ? null
                : $"/api/media/pdf/{x.Id}",

            DurationInMinutes = x.DurationInMinutes,
            Order = x.Order,
            IsPreviewFree = x.IsPreviewFree

        }).ToList();
    }

    public async Task<LessonAccessDto> GetByIdAsync(
     Guid lessonId,
     Guid userId)
    {
        var lesson = await _repo.GetByIdAsync(lessonId);

        if (lesson == null)
            throw new Exception("Lesson not found");

        var isEnrolled = await _enrollmentRepo
            .IsUserEnrolledAsync(userId, lesson.CourseId);

        var dto = new LessonAccessDto
        {
            Id = lesson.Id,
            Title = lesson.Title,

            IsPreviewFree = lesson.IsPreviewFree,

            IsLocked = !isEnrolled &&
                       !lesson.IsPreviewFree,

            // SECURE VIDEO ACCESS
            VideoUrl = (isEnrolled || lesson.IsPreviewFree)
                ? $"/api/media/video/{lesson.Id}"
                : null,

            // SECURE PDF ACCESS
            PdfUrl = (isEnrolled || lesson.IsPreviewFree)
                ? lesson.PdfKey == null
                    ? null
                    : $"/api/media/pdf/{lesson.Id}"
                : null
        };

        return dto;
    }

    public async Task<bool> UpdateAsync(Guid id, UpdateLessonDto dto)
    {
        var lesson = await _repo.GetByIdAsync(id);

        if (lesson == null)
            return false;

        lesson.Title = dto.Title;
        lesson.Description = dto.Description;
        lesson.VideoKey = dto.VideoUrl;
        lesson.PdfKey = dto.PdfUrl;
        lesson.DurationInMinutes = dto.DurationInMinutes;
        lesson.Order = dto.Order;
        lesson.IsPreviewFree = dto.IsPreviewFree;

        await _repo.UpdateAsync(lesson);
        return true;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var lesson = await _repo.GetByIdAsync(id);

        if (lesson == null)
            return false;

        await _repo.DeleteAsync(lesson);
        return true;
    }
}