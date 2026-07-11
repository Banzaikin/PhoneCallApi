using PhoneCallApi.API.Gateway.Interfaces;

namespace PhoneCallApi.API.Gateway.Middleware;

public class ApiGatewayMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IApiGatewayService _gatewayService;
    private readonly ILogger<ApiGatewayMiddleware> _logger;

    public ApiGatewayMiddleware(
        RequestDelegate next,
        IApiGatewayService gatewayService,
        ILogger<ApiGatewayMiddleware> logger)
    {
        _next = next;
        _gatewayService = gatewayService;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (IsExcludedPath(context.Request.Path))
        {
            await _next(context);
            return;
        }

        // 1. Проверка IP белого списка
        if (!await ValidateIPAddress(context))
        {
            await RespondWithError(context, "IP not allowed", 403);
            return;
        }

        // 2. Проверка API Key
        if (!context.Request.Headers.TryGetValue("X-API-Key", out var apiKeyHeader))
        {
            await RespondWithError(context, "API Key is required", 401);
            return;
        }

        var apiKey = apiKeyHeader.FirstOrDefault();
        if (string.IsNullOrEmpty(apiKey))
        {
            await RespondWithError(context, "Invalid API Key", 401);
            return;
        }

        // 3. Валидация API Key
        if (!await _gatewayService.ValidateApiKeyAsync(apiKey))
        {
            await RespondWithError(context, "Invalid API Key", 401);
            return;
        }

        // 4. Проверка rate limiting
        if (await _gatewayService.IsRateLimitedAsync(apiKey))
        {
            await RespondWithError(context, "Rate limit exceeded", 429);
            return;
        }

        await _gatewayService.LogRequestAsync(apiKey, context.Request.Path);

        context.Items["ApiKey"] = apiKey;

        await _next(context);
    }

    private bool IsExcludedPath(PathString path)
    {
        return path.StartsWithSegments("/health") ||
               path.StartsWithSegments("/swagger");
    }

    private async Task<bool> ValidateIPAddress(HttpContext context)
    {
        var remoteIp = context.Connection.RemoteIpAddress?.ToString();
        if (string.IsNullOrEmpty(remoteIp))
            return false;

        return await _gatewayService.ValidateIPAddressAsync(remoteIp);
    }

    private async Task RespondWithError(HttpContext context, string message, int statusCode)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        var response = new
        {
            error = message,
            status = statusCode,
            timestamp = DateTime.UtcNow
        };

        await context.Response.WriteAsJsonAsync(response);
    }
}