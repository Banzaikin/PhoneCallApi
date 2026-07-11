using PhoneCallApi.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace PhoneCallApi.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CallController : ControllerBase
{
    private readonly ICallService _callService;
    private readonly ILogger<CallController> _logger;

    public CallController(ICallService callService, ILogger<CallController> logger)
    {
        _callService = callService;
        _logger = logger;
    }


    /// <summary>
    /// Звонок по телефону
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpPost("make-call")]
    public async Task<IActionResult> MakeCall([FromBody] MakeCallRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _callService.MakeCallAsync(request.PhoneNumber, cancellationToken);
            
            if (result.IsSuccess)
            {
                return Ok(new { 
                    Success = true, 
                    Message = "Call initiated successfully",
                    CallId = result.CallId 
                });
            }
            else
            {
                return BadRequest(new { 
                    Success = false, 
                    Error = result.ErrorMessage 
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error making call to {PhoneNumber}", request.PhoneNumber);
            return StatusCode(500, new { 
                Success = false, 
                Error = "Internal server error" 
            });
        }
    }

    /// <summary>
    /// отправка смс-сообщения по телефону
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpPost("send-sms")]
    public async Task<IActionResult> SendSms([FromBody] SendSmsRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Message))
            {
                return BadRequest(new 
                { 
                    Success = false, 
                    Error = "Текст сообщения не может быть пустым" 
                });
            }
            _logger.LogInformation("Запрос на отправку SMS: номер={PhoneNumber}, длина сообщения={MessageLength}", 
                request.PhoneNumber, request.Message.Length);
            
            var result = await _callService.SendSmsAsync(request.PhoneNumber, request.Message, cancellationToken);
            
            if (result.IsSuccess)
            {
                return Ok(new 
                { 
                    Success = true, 
                    Message = "SMS отправлено успешно",
                    MessageId = result.MessageId,
                    Timestamp = DateTime.UtcNow
                });
            }
            else
            {
                return BadRequest(new 
                { 
                    Success = false, 
                    Error = result.ErrorMessage 
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка отправки SMS на номер {PhoneNumber}", request.PhoneNumber);
            return StatusCode(500, new 
            { 
                Success = false, 
                Error = "Внутренняя ошибка сервера",
                Details = ex.Message
            });
        }
    }
}

public record MakeCallRequest(string PhoneNumber);

public record SendSmsRequest(string PhoneNumber, string Message);