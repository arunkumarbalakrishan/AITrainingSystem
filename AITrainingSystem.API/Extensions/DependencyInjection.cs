using AITrainingSystem.Application.Features.Media.Interfaces;
using AITrainingSystem.Application.Features.Media.Services;
using AITrainingSystem.Application.Interfaces.Auth;
using AITrainingSystem.Application.Interfaces.Lessons;
using AITrainingSystem.Application.Interfaces.Repositories;
using AITrainingSystem.Application.Interfaces.Respository;
using AITrainingSystem.Application.Interfaces.Services;
using AITrainingSystem.Application.Validators.Course;
using AITrainingSystem.Infrastructure.Services.Auth;
using AITrainingSystem.Infrastructure.Services.Courses;
using AITrainingSystem.Infrastructure.Services.Enrollments;
using AITrainingSystem.Infrastructure.Services.Progress;
using AITrainingSystem.Infrastructure.Services.users;
using AITrainingSystem.Persistence.Repositories;
using FluentValidation;


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


        return services;
    }
}