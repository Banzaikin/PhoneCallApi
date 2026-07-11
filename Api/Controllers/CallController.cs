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
}

public record MakeCallRequest(string PhoneNumber);