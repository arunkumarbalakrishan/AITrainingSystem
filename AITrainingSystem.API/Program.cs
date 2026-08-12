using AITrainingSystem.API.Extensions;
using QuestPDF.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Serilog;

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

    // Load Configurations & Environment Settings
    builder.Configuration.AddCustomConfigurationFiles(builder.Environment);

    // Configure Custom Serilog logging
    builder.Host.ConfigureCustomLogging();

    // Add CORS
    builder.Services.AddCustomCors();

    // Configure api behaviors 
    builder.Services.ConfigureCustomApiBehavior();

    // Add Controllers
    builder.Services.AddControllers();
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
