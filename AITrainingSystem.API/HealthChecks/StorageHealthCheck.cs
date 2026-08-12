using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace AITrainingSystem.API.HealthChecks;

public class StorageHealthCheck : IHealthCheck
{
    private readonly IConfiguration _config;

    public StorageHealthCheck(IConfiguration config)
    {
        _config = config;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var provider = _config["Storage:Provider"] ?? "Local";
            if (provider.Equals("Local", StringComparison.OrdinalIgnoreCase))
            {
                var path = Path.Combine(Directory.GetCurrentDirectory(), "Storage");
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                // Attempt test write operations
                var tempFile = Path.Combine(path, $"{Guid.NewGuid()}.tmp");
                await File.WriteAllTextAsync(tempFile, "health check", cancellationToken);
                File.Delete(tempFile);

                return HealthCheckResult.Healthy("Local storage directory is fully writeable.");
            }

            return HealthCheckResult.Healthy($"Storage provider '{provider}' validation skipped in health pipeline.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Storage write operations validation failed.", ex);
        }
    }
}
