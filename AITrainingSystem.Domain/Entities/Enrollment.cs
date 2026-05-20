using AITrainingSystem.Domain.Entities;

public class Enrollment
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }
    public User? User { get; set; }

    public Guid CourseId { get; set; }
    public Course? Course { get; set; }

    public DateTime EnrolledAt { get; set; } = DateTime.UtcNow;
}