namespace Haven.Application.Configuration;

public class NetworkOptions
{
    public const string SectionName = "network";
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 8080;
    public bool EnableTls { get; set; } = false;
}
