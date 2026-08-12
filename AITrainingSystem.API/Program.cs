using AITrainingSystem.API.Extensions;
using QuestPDF.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Sinks.GoogleCloudLogging;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting AITrainingSystem Web API...");

    var builder = WebApplication.CreateBuilder(args);
#if DEBUG
    builder.Environment.EnvironmentName = "Development";
#endif
    QuestPDF.Settings.License = LicenseType.Community;

    var env = builder.Environment;

    // Add CORS
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowAngularDev",
            policy => policy.SetIsOriginAllowed(origin => {
                                var host = new Uri(origin).Host;
                                return host == "localhost" || host.EndsWith(".vercel.app") || host.EndsWith("somee.com");
                             })
                            .AllowAnyHeader()
                            .AllowAnyMethod()
                            .AllowCredentials());
    });

    builder.Configuration.SetBasePath(Directory.GetCurrentDirectory())
                        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                        .AddJsonFile("appsettings.local.json", optional: true, reloadOnChange: true)
                        .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: true);


    builder.Configuration.AddEnvironmentVariables();

    // Bind Serilog integration
    builder.Host.UseSerilog((context, services, configuration) =>
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

    // Add Controllers
    builder.Services.AddControllers();
    builder.Services.Configure<Microsoft.AspNetCore.Mvc.ApiBehaviorOptions>(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var problemDetails = new Microsoft.AspNetCore.Mvc.ValidationProblemDetails(context.ModelState)
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

            return new Microsoft.AspNetCore.Mvc.BadRequestObjectResult(problemDetails)
            {
                ContentTypes = { "application/problem+json" }
            };
        };
    });
    builder.Services.AddSignalR();


    // Database Configuration
    builder.Services.AddDatabase(builder.Configuration);

    //Add validation
    builder.Services.AddValidation();

    // JWT Authentication
    builder.Services.AddJwtAuthentication(builder.Configuration);


    // Authorization
    builder.Services.AddAuthorization();


    // Dependency Injection
    builder.Services.AddApplicationServices(builder.Configuration);

    // Swagger Configuration
    builder.Services.AddSwaggerDocumentation();

    // Register Health Checks
    builder.Services.AddHttpClient(); 
    builder.Services.AddHealthChecks()
        .AddCheck<AITrainingSystem.API.HealthChecks.SqlDatabaseHealthCheck>("database_check")
        .AddCheck<AITrainingSystem.API.HealthChecks.StorageHealthCheck>("storage_check")
        .AddCheck<AITrainingSystem.API.HealthChecks.RedisHealthCheck>("redis_check")
        .AddCheck<AITrainingSystem.API.HealthChecks.AiServiceHealthCheck>("ai_service_check")
        .AddCheck<AITrainingSystem.API.HealthChecks.StripeHealthCheck>("stripe_check");

    var app = builder.Build();

    // Register CorrelationIdMiddleware early in the HTTP pipeline so the correlation ID is available to downstream middleware, controllers, and Serilog logging
    app.UseMiddleware<AITrainingSystem.API.Middleware.CorrelationIdMiddleware>();

    // Register Serilog Request Logging middleware
    app.UseSerilogRequestLogging(options =>
    {
        options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
        options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
        {
            diagnosticContext.Set("Host", httpContext.Request.Host.Value);
            diagnosticContext.Set("ClientIP", httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown");
            
            var correlationId = httpContext.Response.Headers["X-Correlation-ID"].ToString();
            if (!string.IsNullOrEmpty(correlationId))
            {
                diagnosticContext.Set("CorrelationId", correlationId);
            }

            var userId = httpContext.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userId))
            {
                diagnosticContext.Set("UserId", userId);
            }
        };
    });

    // Configure Middleware
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();

        app.UseSwaggerUI();
    }

    app.UseGlobalExceptionHandler();

    if (!app.Environment.IsDevelopment())
    {
        app.UseHttpsRedirection();
    }

    app.UseCors("AllowAngularDev");

    app.UseAuthentication();

    app.UseAuthorization();

    app.MapControllers();
    app.MapHub<AITrainingSystem.API.Hubs.NotificationHub>("/hubs/notifications");

    app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
    {
        ResponseWriter = async (context, report) =>
        {
            context.Response.ContentType = "application/json";

            var json = System.Text.Json.JsonSerializer.Serialize(new
            {
                status = report.Status.ToString(),
                results = report.Entries.ToDictionary(
                    entry => entry.Key,
                    entry => new
                    {
                        status = entry.Value.Status.ToString(),
                        description = entry.Value.Description,
                        duration = entry.Value.Duration.TotalMilliseconds.ToString("0.00") + " ms",
                        error = entry.Value.Exception?.Message
                    }),
                totalDuration = report.TotalDuration.TotalMilliseconds.ToString("0.00") + " ms"
            }, new System.Text.Json.JsonSerializerOptions 
            { 
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
                WriteIndented = true 
            });

            await context.Response.WriteAsync(json);
        }
    });

    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    var maskedConnectionString = string.Join(";", connectionString?.Split(';').Select(p => {
        var parts = p.Split('=');
        if (parts.Length > 0 && (parts[0].Trim().Equals("pwd", StringComparison.OrdinalIgnoreCase) || 
                                 parts[0].Trim().Equals("password", StringComparison.OrdinalIgnoreCase) || 
                                 parts[0].Trim().Equals("user id", StringComparison.OrdinalIgnoreCase))) {
            return parts[0] + "=***";
        }
        return p;
    }) ?? new string[0]);
    Log.Information("Connection String Loaded: {ConnectionString}", maskedConnectionString);

    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        try
        {
            var context = services.GetRequiredService<AITrainingSystem.Persistence.Context.ApplicationDbContext>();
            context.Database.Migrate();
            Log.Information("Database migrated successfully.");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error migrating database.");
        }
    }

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "An unhandled exception occurred during application startup!");
}
finally
{
    Log.CloseAndFlush();
}
