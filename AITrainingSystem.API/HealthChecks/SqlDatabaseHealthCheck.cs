using Microsoft.Extensions.Diagnostics.HealthChecks;
using AITrainingSystem.Persistence.Context;

namespace AITrainingSystem.API.HealthChecks;

public class SqlDatabaseHealthCheck : IHealthCheck
{
    private readonly ApplicationDbContext _context;

    public SqlDatabaseHealthCheck(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            if (await _context.Database.CanConnectAsync(cancellationToken))
            {
                return HealthCheckResult.Healthy("Database is responding and accessible.");
            }
            return HealthCheckResult.Unhealthy("Cannot connect to SQL Server database.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Database connection threw exception.", ex);
        }
    }
}
