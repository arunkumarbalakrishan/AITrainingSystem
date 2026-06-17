using AITrainingSystem.Application.Features.Media.Interfaces;
using AITrainingSystem.Application.Features.Media.Services;
using AITrainingSystem.Application.Interfaces.Auth;
using AITrainingSystem.Application.Interfaces.DashboardService;
using AITrainingSystem.Application.Interfaces.Lessons;
using AITrainingSystem.Application.Interfaces.Repositories;
using AITrainingSystem.Application.Interfaces.Respository;
using AITrainingSystem.Application.Interfaces.Services;
using AITrainingSystem.Application.Services;
using AITrainingSystem.Application.Validators.Course;
using AITrainingSystem.Infrastructure.Services;
using AITrainingSystem.Infrastructure.Services.AI;
using AITrainingSystem.Infrastructure.Services.Assessment;
using AITrainingSystem.Infrastructure.Services.Auth;
using AITrainingSystem.Infrastructure.Services.Courses;
using AITrainingSystem.Infrastructure.Services.Dashboard;
using AITrainingSystem.Infrastructure.Services.Email;
using AITrainingSystem.Infrastructure.Services.Enrollments;
using AITrainingSystem.Infrastructure.Services.Notes;
using AITrainingSystem.Infrastructure.Services.Payments;
using AITrainingSystem.Infrastructure.Services.Progress;
using AITrainingSystem.Infrastructure.Services.Storage;
using AITrainingSystem.Infrastructure.Services.users;
using AITrainingSystem.Persistence.Repositories;
using FluentValidation;


using AITrainingSystem.Infrastructure.Services.LiveClasses;

namespace AITrainingSystem.API.Extensions;

public static class DependencyInjectionExt
{
    public static IServiceCollection AddApplicationServices(
        this IServiceCollection services)
    {
        // Auth Services
        services.AddScoped<IAuthService, AuthService>();

        services.AddScoped<JwtService>();


        // User Services
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IUserService, UserService>();
        
        // Course services
        services.AddValidatorsFromAssemblyContaining<CreateCourseValidator>();
        services.AddScoped<ICourseRepository, CourseRepository>();
        services.AddScoped<ICourseService, CourseService>();

        //Lesson Sevices
        services.AddScoped<ILessonService, LessonService>();
        services.AddScoped<ILessonRepository, LessonRepository>();

        //Enrollment Services
        services.AddScoped<IEnrollmentRepository, EnrollmentRepository>();
        services.AddScoped<IEnrollmentService, EnrollmentService>();

        // Media Services
        services.AddScoped<IMediaAccessService, MediaAccessService>();
        services.AddScoped<IMediaService, MediaService>();


        // Lesson Progress Services
        services.AddScoped<ILessonProgressRepository, LessonProgressRepository>();
        services.AddScoped<ILessonProgressService, LessonProgressService>();

        // Progress Services
        services.AddScoped<IProgressService, ProgressService>();

        services.AddScoped<ICertificateRepository, CertificateRepository>();
        services.AddScoped<ICertificateService, CertificateService>();

        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<ICertificatePdfService, CertificatePdfService>();

        // New Repositories
        services.AddScoped<IQuizRepository, QuizRepository>();
        services.AddScoped<IAssessmentResultRepository, AssessmentResultRepository>();
        services.AddScoped<ILessonNoteRepository, LessonNoteRepository>();
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<IPaymentRepository, PaymentRepository>();

        // New Services
        services.AddScoped<IAssessmentService, AssessmentService>();
        services.AddScoped<ILessonNoteService, LessonNoteService>();
        services.AddScoped<INotificationService, SmtpNotificationService>();
        services.AddScoped<IPaymentService, StripePaymentService>();
        services.AddScoped<ILiveClassService, LiveClassService>();

        // Storage registration
        services.AddScoped<LocalStorageService>();
        services.AddScoped<CloudStorageService>();
        services.AddScoped<IStorageService>(sp =>
        {
            var config = sp.GetRequiredService<Microsoft.Extensions.Configuration.IConfiguration>();
            var provider = config["Storage:Provider"] ?? "Local";
            if (provider.Equals("Local", StringComparison.OrdinalIgnoreCase))
            {
                return sp.GetRequiredService<LocalStorageService>();
            }
            return sp.GetRequiredService<CloudStorageService>();
        });

        // AI Services with HttpClient
        services.AddHttpClient<IAIService, OpenAIService>();

        return services;
    }
}