using AITrainingSystem.Application.Interfaces.Auth;
using AITrainingSystem.Application.Interfaces.Repositories;
using AITrainingSystem.Application.Interfaces.Services;
using AITrainingSystem.Application.Validators.Course;
using AITrainingSystem.Infrastructure.Services;
using AITrainingSystem.Infrastructure.Services.Auth;
using AITrainingSystem.Infrastructure.Services.Course;
using AITrainingSystem.Persistence.Repositories;
using FluentValidation;


namespace AITrainingSystem.API.Extensions;

public static class DependencyInjection
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
        services.AddValidatorsFromAssemblyContaining<CreateCourseValidator>();
        services.AddScoped<ICourseRepository, CourseRepository>();
        services.AddScoped<ICourseService, CourseService>();

        return services;
    }
}