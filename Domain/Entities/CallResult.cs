namespace PhoneCallApi.Domain.Entities;

public record CallResult(bool IsSuccess, string? ErrorMessage = null, Guid? CallId = null);