using AITrainingSystem.Application.Interfaces.Auth;
using AITrainingSystem.Application.Interfaces.Repositories;
using AITrainingSystem.Application.Interfaces.Services;
using AITrainingSystem.Infrastructure.Services;
using AITrainingSystem.Infrastructure.Services.Auth;
using AITrainingSystem.Persistence.Repositories;


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


        return services;
    }
}