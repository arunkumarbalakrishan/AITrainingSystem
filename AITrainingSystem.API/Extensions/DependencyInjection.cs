using AITrainingSystem.Application.Interfaces.Auth;
using AITrainingSystem.Infrastructure.Services.Auth;


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
        


        return services;
    }
}