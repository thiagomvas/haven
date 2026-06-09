namespace Haven.Application.Configuration;

public class NetworkOptions
{
    public const string SectionName = "network";
    public List<string> Domains { get; set; } = [];
    public int Port { get; set; } = 8080;
    public bool EnableTls { get; set; } = false;
}