using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Serilog.Context;

namespace AITrainingSystem.API.Middleware;

public class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;
    private const string CorrelationIdHeaderKey = "X-Correlation-ID";

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // 1. Check if correlation ID was supplied by the client
        if (!context.Request.Headers.TryGetValue(CorrelationIdHeaderKey, out StringValues correlationId))
        {
            correlationId = Guid.NewGuid().ToString();
        }

        // 2. Propagate the ID back to the client in Response headers
        if (!context.Response.Headers.ContainsKey(CorrelationIdHeaderKey))
        {
            context.Response.Headers.Append(CorrelationIdHeaderKey, correlationId);
        }

        // 3. Attach standard Context property for Serilog tracing
        using (LogContext.PushProperty("CorrelationId", correlationId.ToString()))
        {
            await _next(context);
        }
    }
}
