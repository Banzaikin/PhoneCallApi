namespace PhoneCallApi.Application.Contracts;

public class ModemSettings
{
    public const string SectionName = "ModemSettings";
    
    public string PortName { get; set; } = string.Empty;
    public int BaudRate { get; set; } = 9600;
    public int Timeout { get; set; } = 1000;
}