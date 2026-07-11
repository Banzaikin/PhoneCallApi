using PhoneCallApi.Application.Contracts;
using PhoneCallApi.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace PhoneCallApi.Application.Services;

public class CallService : ICallService
{
    private readonly IModemService _modemService;
    private readonly ILogger<CallService> _logger;

    public CallService(
        IModemService modemService,
        ILogger<CallService> logger)
    {
        _modemService = modemService;
        _logger = logger;
    }

    //звонок
    public async Task<CallResult> MakeCallAsync(string phoneNumber, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!await _modemService.TestConnectionAsync())
            {
                var initialized = await _modemService.InitializeAsync(cancellationToken);
                
                if (!initialized)
                {
                    return new CallResult(false, "Ошибка инициализации модема");
                }
            }

            var success = await _modemService.MakeCallAsync(phoneNumber, cancellationToken);
            
            if (success)
            {
                _logger.LogInformation("Звонок на {PhoneNumber} успешно инициализирован", phoneNumber);
                return new CallResult(true, CallId: Guid.NewGuid());
            }
            else
            {
                return new CallResult(false, "Ошибка инициализации номера");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка вызова (тел: {phoneNumber})", phoneNumber);
            return new CallResult(false, ex.Message);
        }
    }

    //отправка смс-сообщений
    public async Task<SmsResult> SendSmsAsync(string phoneNumber, string message, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
            {
                return new SmsResult(false, "Номер телефона не может быть пустым");
            }
            
            if (string.IsNullOrWhiteSpace(message))
            {
                return new SmsResult(false, "Текст сообщения не может быть пустым");
            }
            
            if (message.Length > 160)
            {
                _logger.LogWarning("Сообщение длиннее 160 символов: {Length} символов", message.Length);
            }

            if (!await _modemService.TestConnectionAsync())
            {
                var initialized = await _modemService.InitializeAsync(cancellationToken);
                
                if (!initialized)
                {
                    return new SmsResult(false, "Ошибка инициализации модема");
                }
            }

            var success = await _modemService.SendSmsAsync(phoneNumber, message, cancellationToken);
            
            if (success)
            {
                _logger.LogInformation("SMS успешно отправлено на номер: {PhoneNumber}", phoneNumber);
                return new SmsResult(true, MessageId: Guid.NewGuid());
            }
            else
            {
                return new SmsResult(false, "Ошибка отправки SMS");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка отправки SMS на номер: {PhoneNumber}", phoneNumber);
            return new SmsResult(false, ex.Message);
        }
    }
}