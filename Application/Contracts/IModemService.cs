namespace PhoneCallApi.Application.Contracts;

public interface IModemService
{
    Task<bool> InitializeAsync(CancellationToken cancellationToken = default);
    Task<string> SendCommandAsync(string command, int timeout, CancellationToken cancellationToken = default);
    Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default);
    Task<bool> MakeCallAsync(string phoneNumber, CancellationToken cancellationToken = default);
    Task<bool> SendSmsAsync(string phoneNumber, string message, CancellationToken cancellationToken = default);
}