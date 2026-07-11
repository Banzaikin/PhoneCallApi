using PhoneCallApi.Application.Contracts;
using PhoneCallApi.Domain.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace PhoneCallApi.Application.Services;

public class CallService : ICallService
{
    private readonly IModemService _modemService;
    private readonly ILogger<CallService> _logger;
    private readonly ModemSettings _modemSettings;

    public CallService(
        IModemService modemService,
        ILogger<CallService> logger,
        IOptions<ModemSettings> modemSettings)
    {
        _modemService = modemService;
        _logger = logger;
        _modemSettings = modemSettings.Value;
    }

    public async Task<CallResult> MakeCallAsync(string phoneNumber, CancellationToken cancellationToken = default)
    {
        try
        {
            // Initialize modem if not already initialized
            if (!await _modemService.TestConnectionAsync())
            {
                var initialized = await _modemService.InitializeAsync(cancellationToken);
                
                if (!initialized)
                {
                    return new CallResult(false, "Failed to initialize modem");
                }
            }

            // Make the call through modem service
            var success = await _modemService.MakeCallAsync(phoneNumber, cancellationToken);
            
            if (success)
            {
                _logger.LogInformation("Call to {PhoneNumber} initiated successfully", phoneNumber);
                return new CallResult(true, CallId: Guid.NewGuid());
            }
            else
            {
                return new CallResult(false, "Failed to initiate call");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to make call to {PhoneNumber}", phoneNumber);
            return new CallResult(false, ex.Message);
        }
    }
}