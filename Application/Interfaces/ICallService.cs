using PhoneCallApi.Domain.Entities;

namespace PhoneCallApi.Application.Services;

public interface ICallService
{
    Task<CallResult> MakeCallAsync(string phoneNumber, CancellationToken cancellationToken = default);
    Task<SmsResult> SendSmsAsync(string phoneNumber, string message, CancellationToken cancellationToken = default);
}