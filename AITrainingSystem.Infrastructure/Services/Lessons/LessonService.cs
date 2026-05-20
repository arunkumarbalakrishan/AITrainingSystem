using AITrainingSystem.Application.DTOs.Lesson;
using AITrainingSystem.Application.Interfaces.Lessons;
using AITrainingSystem.Application.Interfaces.Repositories;
using AITrainingSystem.Domain.Entities;

public class LessonService : ILessonService
{
    private readonly ILessonRepository _repo;
    private readonly ICourseRepository _courseRepo;

    public LessonService(ILessonRepository repo, ICourseRepository courseRepo)
    {
        _repo = repo;
        _courseRepo = courseRepo;
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
            VideoUrl = dto.VideoUrl,
            PdfUrl = dto.PdfUrl,
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
            VideoUrl = x.VideoUrl,
            PdfUrl = x.PdfUrl,
            DurationInMinutes = x.DurationInMinutes,
            Order = x.Order,
            IsPreviewFree = x.IsPreviewFree
        }).ToList();
    }

    public async Task<LessonResponseDto> GetByIdAsync(Guid id)
    {
        var x = await _repo.GetByIdAsync(id);

        if (x == null)
            throw new Exception("Lesson not found");

        return new LessonResponseDto
        {
            Id = x.Id,
            CourseId = x.CourseId,
            Title = x.Title,
            Description = x.Description,
            VideoUrl = x.VideoUrl,
            PdfUrl = x.PdfUrl,
            DurationInMinutes = x.DurationInMinutes,
            Order = x.Order,
            IsPreviewFree = x.IsPreviewFree
        };
    }

    public async Task<bool> UpdateAsync(Guid id, UpdateLessonDto dto)
    {
        var lesson = await _repo.GetByIdAsync(id);

        if (lesson == null)
            return false;

        lesson.Title = dto.Title;
        lesson.Description = dto.Description;
        lesson.VideoUrl = dto.VideoUrl;
        lesson.PdfUrl = dto.PdfUrl;
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