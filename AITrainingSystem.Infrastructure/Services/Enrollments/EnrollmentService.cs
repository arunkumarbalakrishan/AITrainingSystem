using AITrainingSystem.Application.Interfaces.Repositories;
using AITrainingSystem.Application.Interfaces.Services;
using AITrainingSystem.Domain.Entities;

namespace AITrainingSystem.Infrastructure.Services.Enrollments;
public class EnrollmentService : IEnrollmentService
{
    private readonly IEnrollmentRepository _enrollmentRepository;

    public EnrollmentService(IEnrollmentRepository enrollmentRepository)
    {
        _enrollmentRepository = enrollmentRepository;
    }

    public async Task EnrollUserAsync(Guid userId, Guid courseId)
    {
        var alreadyEnrolled =
            await _enrollmentRepository.IsUserEnrolledAsync(userId, courseId);

        if (alreadyEnrolled)
        {
            throw new Exception("User already enrolled in this course.");
        }

        var enrollment = new Enrollment
        {
            UserId = userId,
            CourseId = courseId
        };

        await _enrollmentRepository.AddEnrollmentAsync(enrollment);

        await _enrollmentRepository.SaveChangesAsync();
    }
}