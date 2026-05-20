namespace AITrainingSystem.Domain.Entities;

public class Course
{
    public Guid Id { get; set; }

    // Basic Info
    public string Title { get; set; } = null!;
    public string Description { get; set; } = null!;

    // Content Details
    public string? ThumbnailUrl { get; set; }
    public decimal Price { get; set; }

    // Course Meta
    public int DurationInHours { get; set; }
    public bool IsPublished { get; set; } = false;

    // Audit Fields
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Relationships (future expansion)
    public Guid? InstructorId { get; set; }

    public ICollection<Lesson> Lessons { get; set; } = new List<Lesson>();


}