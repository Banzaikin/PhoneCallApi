using System.IO.Ports;
using PhoneCallApi.Application.Contracts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace PhoneCallApi.Infrastructure.Modem;

public class MegafonService : IModemService, IDisposable
{
    private readonly ILogger<MegafonService> _logger;
    private readonly ModemSettings _modemSettings;
    private SerialPort? _serialPort;
    private bool _disposed;
    private bool _initialized;

    public MegafonService(
        ILogger<MegafonService> logger,
        IOptions<ModemSettings> modemSettings)
    {
        _logger = logger;
        _modemSettings = modemSettings.Value;
        
        if (string.IsNullOrEmpty(_modemSettings.PortName))
        {
            throw new ArgumentException("Modem port name is not configured in appsettings.json");
        }
    }

    public async Task<bool> InitializeAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            _logger.LogInformation("Initializing modem on port {PortName}", _modemSettings.PortName);

            _serialPort = new SerialPort(_modemSettings.PortName, _modemSettings.BaudRate)
            {
                Parity = Parity.None,
                DataBits = 8,
                StopBits = StopBits.One,
                Handshake = Handshake.RequestToSend,
                ReadTimeout = _modemSettings.Timeout,
                WriteTimeout = _modemSettings.Timeout
            };

            _serialPort.Open();
            
            // Test modem connection
            var response = await SendCommandAsync("AT", _modemSettings.Timeout, cancellationToken);
            _initialized = response.Contains("OK");
            
            if (_initialized)
            {
                _logger.LogInformation("Modem initialized successfully on port {PortName}", _modemSettings.PortName);
            }
            
            return _initialized;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Modem initialization was cancelled");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize modem on port {PortName}", _modemSettings.PortName);
            _initialized = false;
            return false;
        }
    }

    public async Task<string> SendCommandAsync(string command, int timeout, CancellationToken cancellationToken = default)
    {
        if (_serialPort == null || !_serialPort.IsOpen)
            throw new InvalidOperationException("Modem is not initialized");

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            _serialPort.ReadTimeout = timeout;
            _serialPort.DiscardInBuffer();
            _serialPort.DiscardOutBuffer();

            _serialPort.WriteLine(command + "\r");
            
            // Используем задержку с поддержкой cancellation
            await Task.Delay(100, cancellationToken);
            
            var response = _serialPort.ReadExisting();
            _logger.LogDebug("Command: {Command}, Response: {Response}", command, response);
            
            return response;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Command {Command} was cancelled", command);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending command: {Command}", command);
            throw;
        }
    }

    public async Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        if (!_initialized)
            return false;

        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var response = await SendCommandAsync("AT", _modemSettings.Timeout, cancellationToken);
            return response.Contains("OK");
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Test connection was cancelled");
            throw;
        }
        catch
        {
            _initialized = false;
            return false;
        }
    }

    public async Task<bool> MakeCallAsync(string phoneNumber, CancellationToken cancellationToken = default)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            _logger.LogInformation("Making call to {PhoneNumber}", phoneNumber);

            // Set text mode
            await SendCommandAsync("AT+CMGF=1", _modemSettings.Timeout, cancellationToken);
            
            // Make the call
            var response = await SendCommandAsync($"ATD{phoneNumber};", _modemSettings.Timeout, cancellationToken);
            
            var success = response.Contains("OK") || response.Contains("CONNECT");
            
            if (success)
            {
                _logger.LogInformation("Call to {PhoneNumber} initiated successfully", phoneNumber);
            }
            else
            {
                _logger.LogWarning("Call to {PhoneNumber} failed. Response: {Response}", phoneNumber, response);
            }
            
            return success;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Call to {PhoneNumber} was cancelled", phoneNumber);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error making call to {PhoneNumber}", phoneNumber);
            return false;
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _serialPort?.Close();
            _serialPort?.Dispose();
            _disposed = true;
            _initialized = false;
            _logger.LogInformation("Modem service disposed");
        }
    }
}