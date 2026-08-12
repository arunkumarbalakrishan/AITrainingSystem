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

    public async Task<T?> GetAsync<T>(string key)
    {
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
            _logger.LogWarning(ex, "Redis connection failed during GetAsync for key: {CacheKey}. Falling back to DB.", key);
            return default;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expirationTime = null)
    {
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
            _logger.LogWarning(ex, "Redis connection failed during SetAsync for key: {CacheKey}.", key);
        }
    }

    public async Task RemoveAsync(string key)
    {
        try
        {
            await _cache.RemoveAsync(key);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis connection failed during RemoveAsync for key: {CacheKey}.", key);
        }
    }
}
