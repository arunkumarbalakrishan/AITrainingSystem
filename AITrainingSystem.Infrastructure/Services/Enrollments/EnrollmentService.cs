using System;
using System.Threading.Tasks;
using AITrainingSystem.Application.Interfaces.Repositories;
using AITrainingSystem.Application.Interfaces.Services;
using AITrainingSystem.Domain.Entities;
using AITrainingSystem.Infrastructure.Services.Email;

namespace AITrainingSystem.Infrastructure.Services.Enrollments
{
    public class EnrollmentService : IEnrollmentService
    {
        private readonly IEnrollmentRepository _enrollmentRepository;
        private readonly IUserRepository _userRepository;
        private readonly ICourseRepository _courseRepository;
        private readonly INotificationService _notificationService;

        public EnrollmentService(
            IEnrollmentRepository enrollmentRepository,
            IUserRepository userRepository,
            ICourseRepository courseRepository,
            INotificationService notificationService)
        {
            _enrollmentRepository = enrollmentRepository;
            _userRepository = userRepository;
            _courseRepository = courseRepository;
            _notificationService = notificationService;
        }

        public async Task EnrollUserAsync(Guid userId, Guid courseId)
        {
            var alreadyEnrolled = await _enrollmentRepository.IsUserEnrolledAsync(userId, courseId);

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

            // Decoupled Background Receipt Email & In-App Notification
            try
            {
                var user = await _userRepository.GetByIdAsync(userId);
                var course = await _courseRepository.GetByIdAsync(courseId);

                if (user != null && course != null)
                {
                    await _notificationService.CreateInAppNotificationAsync(
                        userId,
                        "Course Enrollment Confirmed! 🎉",
                        $"You have successfully enrolled in '{course.Title}'. Start learning anytime from your dashboard."
                    );
                }
            }
            catch
            {
                // Silently absorb notification trigger failures to keep core enrollment transaction 100% resilient
            }
        }
    }
}