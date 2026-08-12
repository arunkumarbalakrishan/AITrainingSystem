using Microsoft.AspNetCore.Mvc;
using Serilog;
using Serilog.Sinks.GoogleCloudLogging;

namespace AITrainingSystem.API.Extensions;

public static class ConfigurationExtensions
{
    public static ConfigurationManager AddCustomConfigurationFiles(this ConfigurationManager configuration, IWebHostEnvironment env)
    {
        configuration.SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .AddJsonFile("appsettings.local.json", optional: true, reloadOnChange: false)
            .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: false);

        configuration.AddEnvironmentVariables();
        return configuration;
    }

    public static IServiceCollection AddCustomCors(this IServiceCollection services)
    {
        services.AddCors(options =>
        {
            options.AddPolicy("AllowAngularDev",
                policy => policy.SetIsOriginAllowed(origin => 
                    {
                        var host = new Uri(origin).Host;
                        return host == "localhost" || host.EndsWith(".vercel.app") || host.EndsWith("somee.com");
                    })
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials());
        });

        return services;
    }

    public static IHostBuilder ConfigureCustomLogging(this IHostBuilder host)
    {
        host.UseSerilog((context, services, configuration) =>
        {
            configuration
                .ReadFrom.Configuration(context.Configuration)
                .ReadFrom.Services(services);

            var firebaseProjectId = context.Configuration["Firebase:ProjectId"];
            var hasGoogleCredentials = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS"))
                                    || File.Exists(Path.Combine(context.HostingEnvironment.ContentRootPath, "firebase-credentials.json"))
                                    || File.Exists("firebase-credentials.json");

            if (!string.IsNullOrEmpty(firebaseProjectId) && hasGoogleCredentials)
            {
                configuration.WriteTo.GoogleCloudLogging(new GoogleCloudLoggingSinkOptions(firebaseProjectId)
                {
                    LogName = "aitrainingsystem-api-logs"
                });
            }
        });

        return host;
    }

    public static IServiceCollection ConfigureCustomApiBehavior(this IServiceCollection services)
    {
        services.Configure<ApiBehaviorOptions>(options =>
        {
            options.InvalidModelStateResponseFactory = context =>
            {
                var problemDetails = new ValidationProblemDetails(context.ModelState)
                {
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                    Title = "One or more validation errors occurred.",
                    Status = StatusCodes.Status400BadRequest,
                    Detail = "See the errors property for details.",
                    Instance = context.HttpContext.Request.Path
                };

                var correlationId = context.HttpContext.Response.Headers["X-Correlation-ID"].ToString();
                if (!string.IsNullOrEmpty(correlationId))
                {
                    problemDetails.Extensions["traceId"] = correlationId;
                }

                return new BadRequestObjectResult(problemDetails)
                {
                    ContentTypes = { "application/problem+json" }
                };
            };
        });

        return services;
    }
}
