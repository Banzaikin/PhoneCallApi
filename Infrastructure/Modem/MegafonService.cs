using System.IO.Ports;
using PhoneCallApi.Application.Contracts;
using PhoneCallApi.Domain.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text;
using Lers.Utils.Sms;

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
            throw new ArgumentException("Неверно указан порт модема в appsettings.json");
        }
    }

    public async Task<bool> InitializeAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            _logger.LogInformation("Инициализация модема: {PortName}", _modemSettings.PortName);

            _serialPort = new SerialPort(_modemSettings.PortName, _modemSettings.BaudRate)
            {
                Parity = Parity.None,
                DataBits = 8,
                StopBits = StopBits.One,
                Handshake = Handshake.None,
                ReadTimeout = _modemSettings.Timeout,
                WriteTimeout = _modemSettings.Timeout
            };

            _serialPort.Open();
            
            // Тестовый коннект
            var response = await SendCommandAsync("AT", _modemSettings.Timeout, cancellationToken);
            _initialized = response.Contains("OK");
            
            if (_initialized)
            {
                _logger.LogInformation("Модем успешно инициализирован: {PortName}", _modemSettings.PortName);
            }
            
            return _initialized;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Операция отменена");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка инициализации порта: {PortName}", _modemSettings.PortName);
            _initialized = false;
            return false;
        }
    }

    public async Task<string> SendCommandAsync(string command, int timeout, CancellationToken cancellationToken = default)
    {
        if (_serialPort == null || !_serialPort.IsOpen)
            throw new InvalidOperationException("Модем не инициализирован");

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            _serialPort.ReadTimeout = timeout;
            _serialPort.DiscardInBuffer();
            _serialPort.DiscardOutBuffer();

            _serialPort.WriteLine(command + "\r");

            var buffer = new StringBuilder();
            var start = DateTime.Now;

            while ((DateTime.Now - start).TotalMilliseconds < timeout)
            {
                try
                {
                    var line = _serialPort.ReadLine();
                    buffer.AppendLine(line);

                    // Модем ждет ответа при завершении звонка
                    if (line.Contains("OK") || line.Contains("ERROR") || line.Contains("NO CARRIER") || line.Contains("BUSY"))
                        break;
                }
                catch (TimeoutException)
                {
                    break;
                }
                await Task.Delay(50, cancellationToken);
            }

            var response = buffer.ToString();
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
            _logger.LogError(ex, "Ошибка отправки команды: {Command}", command);
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
            _logger.LogWarning("Тестовый коннект был отменен");
            throw;
        }
        catch
        {
            _initialized = false;
            return false;
        }
    }

    //звонки с модема
    public async Task<bool> MakeCallAsync(string phoneNumber, CancellationToken cancellationToken = default)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            _logger.LogInformation("Вызов на тел: {PhoneNumber}", phoneNumber);

            await SendCommandAsync("ATH0", _modemSettings.Timeout, cancellationToken);
            // Вызов
            var response = await SendCommandAsync($"ATD{phoneNumber};", _modemSettings.Timeout, cancellationToken);
            
            var success = response.Contains("OK") || response.Contains("CONNECT");
            
            if (success)
            {
                _logger.LogInformation("Вызов на тел: {PhoneNumber} - успешно", phoneNumber);
            }
            else
            {
                _logger.LogWarning("Вызов на тел: {PhoneNumber} - ошибка. Response: {Response}", phoneNumber, response);
            }
            
            return success;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Вызов на тел: {PhoneNumber} отменен", phoneNumber);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error making call to {PhoneNumber}", phoneNumber);
            return false;
        }
    }

    //отправка смс-сообщений
    public async Task<bool> SendSmsAsync(string phoneNumber, string message, CancellationToken cancellationToken = default)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            if (string.IsNullOrEmpty(message))
            {
                _logger.LogError("Текст сообщения пустой");
                return false;
            }

            _logger.LogInformation("Отправка SMS на номер: {PhoneNumber}, длина: {Length}", 
                phoneNumber, message.Length);

            if (!await TestConnectionAsync(cancellationToken))
            {
                var initialized = await InitializeAsync(cancellationToken);
                if (!initialized)
                {
                    _logger.LogError("Не удалось инициализировать модем для отправки SMS");
                    return false;
                }
            }

            //Устанавливаем PDU режим
            var atResponse = await SendCommandAsync("AT+CMGF=0", _modemSettings.Timeout, cancellationToken);
            if (!atResponse.Contains("OK"))
            {
                _logger.LogError("Не удалось установить PDU режим. Response: {Response}", atResponse);
                return false;
            }

            //Генерируем PDU строку с помощью Lers.Utils.Sms
            //Используем SmsEncoding.UCS2 для поддержки кириллицы
            string pdu = SmsPduBuilder.BuildPdu(
                encoding: SmsEncoding.UCS2,    // Кодировка для кириллицы
                phoneNumber: phoneNumber,
                smsText: message,
                partNumber: 0,                 // 0 - не разбито на части
                partCount: 0,                  // 0 - не разбито на части  
                referenceNumber: 0             // 0 - не используется
            );
            
            int pduLength = CalculatePduLength(pdu);
            
            _logger.LogDebug("PDU строка: {Pdu}, длина: {Length}", pdu, pduLength);

            var smsCommand = $"AT+CMGS={pduLength}";
            
            _serialPort?.DiscardInBuffer();
            _serialPort?.DiscardOutBuffer();

            _serialPort?.Write(smsCommand + "\r");

            await Task.Delay(1500, cancellationToken);

            _serialPort?.Write(pdu + "\x1A");

            var startTime = DateTime.Now;
            var responseBuilder = new StringBuilder();
            bool smsSent = false;

            while ((DateTime.Now - startTime).TotalMilliseconds < 45000)
            {
                if (_serialPort?.BytesToRead > 0)
                {
                    try
                    {
                        var line = _serialPort.ReadLine();
                        responseBuilder.AppendLine(line);
                        _logger.LogDebug("SMS response line: {Line}", line);

                        if (line.Contains("+CMGS:") || line.Contains("OK"))
                        {
                            smsSent = true;
                            _logger.LogInformation("SMS отправлено успешно. Ответ: {Response}", line);
                            break;
                        }
                        else if (line.Contains("ERROR") || line.Contains("CMS ERROR"))
                        {
                            _logger.LogError("Ошибка отправки SMS. Response: {Response}", responseBuilder.ToString());
                            return false;
                        }
                    }
                    catch (TimeoutException)
                    {
                        continue;
                    }
                }
                await Task.Delay(500, cancellationToken);
            }

            if (smsSent)
            {
                _logger.LogInformation("SMS успешно отправлено на номер: {PhoneNumber}", phoneNumber);
                return true;
            }
            else
            {
                _logger.LogError("Таймаут отправки SMS. Response: {Response}", responseBuilder.ToString());
                return false;
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Отправка SMS отменена для номера: {PhoneNumber}", phoneNumber);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка отправки SMS на номер: {PhoneNumber}", phoneNumber);
            return false;
        }
    }

    // Метод для вычисления длины PDU в октетах
    private int CalculatePduLength(string pdu)
    {
        try
        {
            // Формат PDU: [SMSC][TPDU]
            // Длина PDU для команды AT+CMGS = длина TPDU в октетах
            
            // Если SMSC указан как "00" - это пустой SMSC (1 октет)
            if (pdu.StartsWith("00"))
            {
                // Пример: "0011000B91..." 
                // 00 - SMSC длина (0 = 1 октет)
                // Остальная часть - TPDU
                // Длина TPDU = (общая длина / 2) - 1
                return (pdu.Length / 2) - 1;
            }
            else
            {
                // SMSC указан, нужно вычислить его длину
                // Первый октет - длина SMSC в октетах (не включая сам этот байт)
                int smscLength = Convert.ToInt32(pdu.Substring(0, 2), 16);
                
                // Длина TPDU = (общая длина в октетах) - smscLength - 1
                return (pdu.Length / 2) - smscLength - 1;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка вычисления длины PDU: {Pdu}", pdu);

            // Большинство PDU начинаются с "00" для пустого SMSC
            return (pdu.Length / 2) - 1;
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