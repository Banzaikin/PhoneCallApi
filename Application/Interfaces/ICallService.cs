using PhoneCallApi.Domain.Entities;

namespace PhoneCallApi.Application.Services;

public interface ICallService
{
    Task<CallResult> MakeCallAsync(string phoneNumber, CancellationToken cancellationToken = default);
}