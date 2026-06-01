namespace AITrainingSystem.Application.DTOs.Analytics;

public class RecentlyCompletedCourseDto
{
    public Guid CourseId { get; set; }

    public string CourseTitle { get; set; } = string.Empty;

    public DateTime CompletedAt { get; set; }
}