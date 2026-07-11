using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using PhoneCallApi.API.Gateway.Interfaces;
using PhoneCallApi.Application.Contracts;

namespace PhoneCallApi.API.Gateway.Services;

public class ApiGatewayService : IApiGatewayService
{
    private readonly IMemoryCache _cache;
    private readonly ApiGatewaySettings _settings;
    private readonly ILogger<ApiGatewayService> _logger;

    public ApiGatewayService(
        IMemoryCache cache,
        IOptions<ApiGatewaySettings> settings,
        ILogger<ApiGatewayService> logger)
    {
        _cache = cache;
        _settings = settings.Value;
        _logger = logger;
    }

    public Task<bool> ValidateApiKeyAsync(string apiKey)
    {
        var isValid = apiKey == _settings.MasterApiKey || 
                     _settings.ClientApiKeys.Contains(apiKey);
        
        return Task.FromResult(isValid);
    }

    public Task<bool> IsRateLimitedAsync(string apiKey)
    {
        var cacheKey = $"ratelimit_{apiKey}";
        
        if (_cache.TryGetValue(cacheKey, out int requestCount))
        {
            if (requestCount >= _settings.MaxRequestsPerMinute)
            {
                return Task.FromResult(true);
            }
        }
        
        return Task.FromResult(false);
    }

    public Task LogRequestAsync(string apiKey, string endpoint)
    {
        var cacheKey = $"ratelimit_{apiKey}";
        var options = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1)
        };

        _cache.TryGetValue(cacheKey, out int currentCount);
        _cache.Set(cacheKey, currentCount + 1, options);

        _logger.LogInformation("API Request - Key: {ApiKey}, Endpoint: {Endpoint}", 
            apiKey[..4] + "***", endpoint);
        
        return Task.CompletedTask;
    }

    public Task<bool> ValidateIPAddressAsync(string ipAddress)
    {
        if (!_settings.EnableIPWhitelist)
            return Task.FromResult(true);

        return Task.FromResult(_settings.AllowedIPs.Contains(ipAddress));
    }
}