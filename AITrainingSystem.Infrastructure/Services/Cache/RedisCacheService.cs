using AITrainingSystem.Application.Interfaces.Services;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace AITrainingSystem.Infrastructure.Services.Cache;

public class RedisCacheService : ICacheService
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<RedisCacheService> _logger;
    
    // Circuit breaker state variables to handle runtime Redis failures
    private static bool _isRedisHealthy = true;
    private static DateTime _lastFallbackTime = DateTime.MinValue;
    private static readonly TimeSpan CircuitBreakerDuration = TimeSpan.FromMinutes(2);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public RedisCacheService(IDistributedCache cache, ILogger<RedisCacheService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    private bool CheckCacheHealth()
    {
        if (!_isRedisHealthy)
        {
            if (DateTime.UtcNow - _lastFallbackTime < CircuitBreakerDuration)
            {
                return false; // Circuit is open, bypass Redis
            }
            _isRedisHealthy = true; // Retry connection after duration
        }
        return true;
    }

    private void HandleCacheFailure(Exception ex, string operation, string key)
    {
        _isRedisHealthy = false;
        _lastFallbackTime = DateTime.UtcNow;
        _logger.LogWarning(ex, "Redis connection failed during {Operation} for key: {CacheKey}. Circuit breaker opened; bypassing Redis cache temporarily.", operation, key);
    }

    public async Task<T?> GetAsync<T>(string key)
    {
        if (!CheckCacheHealth())
        {
            return default;
        }

        try
        {
            var json = await _cache.GetStringAsync(key);
            if (string.IsNullOrEmpty(json))
            {
                return default;
            }
            return JsonSerializer.Deserialize<T>(json, JsonOptions);
        }
        catch (Exception ex)
        {
            HandleCacheFailure(ex, "GetAsync", key);
            return default;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expirationTime = null)
    {
        if (!CheckCacheHealth())
        {
            return;
        }

        try
        {
            var json = JsonSerializer.Serialize(value, JsonOptions);
            var options = new DistributedCacheEntryOptions();

            if (expirationTime.HasValue)
            {
                options.SetAbsoluteExpiration(expirationTime.Value);
            }
            else
            {
                // Default fallback of 30 minutes
                options.SetAbsoluteExpiration(TimeSpan.FromMinutes(30));
            }

            await _cache.SetStringAsync(key, json, options);
        }
        catch (Exception ex)
        {
            HandleCacheFailure(ex, "SetAsync", key);
        }
    }

    public async Task RemoveAsync(string key)
    {
        if (!CheckCacheHealth())
        {
            return;
        }

        try
        {
            await _cache.RemoveAsync(key);
        }
        catch (Exception ex)
        {
            HandleCacheFailure(ex, "RemoveAsync", key);
        }
    }
}
