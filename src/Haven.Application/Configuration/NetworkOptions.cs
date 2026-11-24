namespace Haven.Application.Configuration;

public class NetworkOptions
{
    public const string SectionName = "network";
    public List<string> Domains { get; set; } = [];
    public int Port { get; set; } = 8080;
    public bool EnableTls { get; set; } = false;

    /// <summary>
    /// IPv4 CIDR block Haven carves per-environment /24 Docker networks out of. Deliberately outside
    /// Docker's own auto-allocation pools (172.17-31.0.0/16, 192.168.0.0/16); change it if it clashes
    /// with your LAN or VPN.
    /// </summary>
    public string SubnetPool { get; set; } = "10.240.0.0/12";

    /// <summary>
    /// Automatically recreate Haven networks whose Docker subnet lies outside <see cref="SubnetPool"/>
    /// once nothing is attached to them. Networks in use are only reported, never touched.
    /// </summary>
    public bool AutoRepairIdleNetworks { get; set; } = true;

    /// <summary>
    /// Remove Docker networks carrying Haven's managed label that no Haven network record refers to
    /// anymore, once older than <see cref="OrphanNetworkGracePeriodHours"/>. Off by default; the health
    /// report still lists them.
    /// </summary>
    public bool RemoveOrphanedDockerNetworks { get; set; } = false;

    public int OrphanNetworkGracePeriodHours { get; set; } = 24;

    public string? BuildHost()
    {
        if (Domains.Count == 0)
            return null;

        return BuildOrigin(Domains[0]);
    }

    /// <summary>
    /// Turns a configured domain (bare host or a full http(s) URL) into an origin such as
    /// <c>https://example.com</c> or <c>http://example.com:8080</c>.
    /// </summary>
    public string BuildOrigin(string domain)
    {
        domain = domain.Trim();

        if (domain.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            domain.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            return domain.TrimEnd('/');

        var scheme = EnableTls ? "https" : "http";
        var defaultPort = EnableTls ? 443 : 80;
        return Port != defaultPort ? $"{scheme}://{domain}:{Port}" : $"{scheme}://{domain}";
    }

    public string BuildEndpointRoute(string route)
    {
        var host = BuildHost();
        if (host == null)
            throw new InvalidOperationException("No domain is configured.");

        return $"{host}/{route.TrimStart('/')}";
    }
}