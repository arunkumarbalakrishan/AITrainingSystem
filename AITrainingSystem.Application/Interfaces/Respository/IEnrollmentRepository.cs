using AITrainingSystem.Domain.Entities;

namespace AITrainingSystem.Application.Interfaces.Repositories;

public interface IEnrollmentRepository
{
    Task<bool> IsUserEnrolledAsync(Guid userId, Guid courseId);

    Task AddEnrollmentAsync(Enrollment enrollment);

    Task<Enrollment?> GetEnrollmentAsync(Guid userId, Guid courseId);

    Task<int> GetEnrolledCourseCountAsync(Guid userId);
    Task SaveChangesAsync();
}