using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Net.Sockets;

namespace AITrainingSystem.API.HealthChecks;

public class RedisHealthCheck : IHealthCheck
{
    private readonly IConfiguration _config;

    public RedisHealthCheck(IConfiguration config)
    {
        _config = config;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var connectionString = _config["Redis:ConnectionString"] ?? "localhost:6379";
        var parts = connectionString.Split(':');
        var host = parts[0];
        var port = parts.Length > 1 && int.TryParse(parts[1], out var p) ? p : 6379;

        try
        {
            using var tcpClient = new TcpClient();
            var connectTask = tcpClient.ConnectAsync(host, port, cancellationToken).AsTask();
            var delayTask = Task.Delay(2000, cancellationToken); // 2 second timeout

            var completedTask = await Task.WhenAny(connectTask, delayTask);
            if (completedTask == delayTask)
            {
                return HealthCheckResult.Degraded($"Redis connection timed out on {host}:{port}. Caching may be sluggish.");
            }

            if (tcpClient.Connected)
            {
                return HealthCheckResult.Healthy($"Redis server is responding on socket {host}:{port}.");
            }

            return HealthCheckResult.Unhealthy($"Cannot reach Redis server socket on {host}:{port}.");
        }
        catch (Exception)
        {
            return HealthCheckResult.Degraded($"Redis socket connection failed on {host}:{port}. Caching deactivated.");
        }
    }
}
