namespace AITrainingSystem.Application.DTOs.Course;

public class CreateCourseDto
{
    public string Title { get; set; } = null!;

    public string Description { get; set; } = null!;

    public decimal Price { get; set; }

    public int DurationInHours { get; set; }

    public string? ThumbnailUrl { get; set; }
}