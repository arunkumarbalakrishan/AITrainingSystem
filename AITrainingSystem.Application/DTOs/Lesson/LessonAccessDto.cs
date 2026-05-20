public class LessonAccessDto
{
    public Guid Id { get; set; }

    public string Title { get; set; }

    public bool IsLocked { get; set; }

    public bool IsPreviewFree { get; set; }

    public string? VideoUrl { get; set; }
}