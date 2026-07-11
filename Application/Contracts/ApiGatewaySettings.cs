namespace PhoneCallApi.Application.Contracts;

public class ApiGatewaySettings
{
    public const string SectionName = "ApiGatewaySettings";
    
    public string MasterApiKey { get; set; } = string.Empty;
    public List<string> ClientApiKeys { get; set; } = new();
    public int MaxRequestsPerMinute { get; set; } = 60;
    public List<string> AllowedIPs { get; set; } = new();
    public bool EnableIPWhitelist { get; set; } = false;
}