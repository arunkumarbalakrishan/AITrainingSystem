namespace AITrainingSystem.Application.DTOs.Course;

using AITrainingSystem.Application.DTOs.Lesson;

public class CourseFullDto
{
    public Guid Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public List<LessonAccessDto> Lessons { get; set; } = new();
}