using FluentValidation;
using FluentValidation.AspNetCore;
using System.Reflection;

namespace AITrainingSystem.API.Extensions;

public static class ValidationExtensions
{
    public static IServiceCollection AddValidation(
        this IServiceCollection services)
    {
        services.AddFluentValidationAutoValidation();

        services.AddValidatorsFromAssembly(
            Assembly.Load("AITrainingSystem.Application")
        );

        return services;
    }
}