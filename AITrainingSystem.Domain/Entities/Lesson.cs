namespace AITrainingSystem.Domain.Entities;

public class Lesson
{
    public Guid Id { get; set; }

    public Guid CourseId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string VideoKey { get; set; } = string.Empty;

    public string? PdfKey { get; set; }

    public int DurationInMinutes { get; set; }

    public int Order { get; set; }

    public bool IsPreviewFree { get; set; } 

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation Property
    public Course Course { get; set; } = null!;

    public ICollection<LessonProgress> Progresses { get; set; } = new List<LessonProgress>();

}