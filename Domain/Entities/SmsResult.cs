namespace PhoneCallApi.Domain.Entities;

public record SmsResult(bool IsSuccess, string? ErrorMessage = null, Guid? MessageId = null);