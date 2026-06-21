namespace Haven.Application.Configuration;

public class NetworkOptions
{
    public const string SectionName = "network";
    public List<string> Domains { get; set; } = [];
    public int Port { get; set; } = 8080;
    public bool EnableTls { get; set; } = false;

    public string? BuildHost()
    {
        if (Domains.Count == 0)
            return null;

        var domain = Domains[0];

        if (domain.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            domain.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            return domain.TrimEnd('/');

        var scheme = EnableTls ? "https" : "http";
        var defaultPort = EnableTls ? 443 : 80;
        return Port != defaultPort ? $"{scheme}://{domain}:{Port}" : $"{scheme}://{domain}";
    }
}