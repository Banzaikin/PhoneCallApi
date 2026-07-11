namespace PhoneCallApi.API.Gateway.Interfaces;

public interface IApiGatewayService
{
    Task<bool> ValidateApiKeyAsync(string apiKey);
    Task<bool> IsRateLimitedAsync(string apiKey);
    Task LogRequestAsync(string apiKey, string endpoint);
    Task<bool> ValidateIPAddressAsync(string ipAddress);
}