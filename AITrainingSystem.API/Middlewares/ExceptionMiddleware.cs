using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using FluentValidation;

namespace AITrainingSystem.API.Middleware;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;
    private readonly IHostEnvironment _env;

    public ExceptionMiddleware(
        RequestDelegate next,
        ILogger<ExceptionMiddleware> logger,
        IHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var statusCode = HttpStatusCode.InternalServerError;
        var title = "An unexpected error occurred";
        
        // Exclude system message details in Production to keep responses secure
        var detail = _env.IsDevelopment() 
            ? exception.Message 
            : "An unexpected error occurred on the server.";
            
        var type = "https://tools.ietf.org/html/rfc7231#section-6.6.1";
        IDictionary<string, string[]>? errors = null;

        // Map exception types to HTTP status codes
        switch (exception)
        {
            case ValidationException valEx:
                statusCode = HttpStatusCode.BadRequest;
                title = "Validation failed";
                detail = "One or more validation constraints were violated.";
                type = "https://tools.ietf.org/html/rfc7231#section-6.5.1";
                errors = valEx.Errors
                    .GroupBy(k => k.PropertyName)
                    .ToDictionary(g => g.Key ?? string.Empty, g => g.Select(x => x.ErrorMessage).ToArray());
                break;

            case UnauthorizedAccessException:
                statusCode = HttpStatusCode.Unauthorized;
                title = "Unauthorized";
                detail = "Permission credentials or token expired/invalid.";
                type = "https://tools.ietf.org/html/rfc7235#section-3.1";
                break;

            case KeyNotFoundException keyEx:
                statusCode = HttpStatusCode.NotFound;
                title = "Resource not found";
                detail = keyEx.Message; // Generic resource exceptions are safe to share
                type = "https://tools.ietf.org/html/rfc7231#section-6.5.4";
                break;
                
            // Handle custom forbidden exceptions if thrown by the services layer
            case InvalidOperationException invEx when invEx.Message.Contains("forbidden", StringComparison.OrdinalIgnoreCase):
                statusCode = HttpStatusCode.Forbidden;
                title = "Forbidden";
                detail = "Access to the requested resource is denied.";
                type = "https://tools.ietf.org/html/rfc7231#section-6.5.3";
                break;
        }

        // Retrieve active correlation ID from Response headers
        var correlationId = context.Response.Headers["X-Correlation-ID"].ToString();
        if (string.IsNullOrEmpty(correlationId))
        {
            correlationId = context.TraceIdentifier;
        }

        // FULL detailed exception + Trace Logged safely to Serilog / Seq
        _logger.LogError(
            exception,
            "An error occurred: {ErrorMessage} [Path: {Path}] [CorrelationId: {CorrelationId}]",
            exception.Message,
            context.Request.Path,
            correlationId
        );

        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = (int)statusCode;

        var problemDetails = new ProblemDetails
        {
            Status = (int)statusCode,
            Title = title,
            Type = type,
            Detail = detail,
            Instance = context.Request.Path
        };

        // Attach correlation metadata
        problemDetails.Extensions["traceId"] = correlationId;

        // Dev-only debug data details (filtered out in Production)
        if (_env.IsDevelopment())
        {
            problemDetails.Extensions["exception"] = exception.ToString();
        }

        if (errors != null)
        {
            problemDetails.Extensions["errors"] = errors;
        }

        var json = JsonSerializer.Serialize(problemDetails, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(json);
    }
}