using Haven.Application.Common;

namespace Haven.Application.Common.Interfaces.Services;

/// <summary>
/// Queries the Traefik sidecar's REST API over Haven's internal, non-public entrypoint (see
/// <c>DockerUtils.EnsureHavenInternalTraefikArgs</c>) for on-demand router/TLS status. Never
/// throws on unreachability - callers degrade the UI ("Traefik unreachable") instead of erroring.
/// </summary>
public interface ITraefikApiClient
{
    Task<Result<bool>> IsReachableAsync(CancellationToken ct = default);

    /// <summary>
    /// Fetches Traefik's live view of a router by name (as built by <c>DockerUtils.BuildTraefikLabels</c>).
    /// Returns a failure result if Traefik is unreachable or the router isn't (yet) known to it.
    /// </summary>
    Task<Result<TraefikRouterInfo>> GetRouterInfoAsync(string routerName, CancellationToken ct = default);

    /// <summary>
    /// Fetches the backend server URL(s) Traefik is <em>actually</em> resolving for a service by name
    /// (the same name as its router - see <c>ServiceRegistryDomain.RouterName</c>), straight from
    /// Traefik's own runtime state via its API. Used to detect drift between what Traefik is really
    /// routing to and where a container's IP actually is now - which Docker network membership alone
    /// can't reveal, since Traefik's Docker provider doesn't refresh on network connect/disconnect
    /// events, only on container lifecycle events. Returns a failure result if Traefik is unreachable
    /// or the service isn't (yet) known to it.
    /// </summary>
    Task<Result<IReadOnlyList<string>>> GetServiceServerUrlsAsync(string serviceName, CancellationToken ct = default);
}

public sealed class TraefikRouterInfo
{
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public bool HasTls { get; set; }
    public List<string> Errors { get; set; } = [];
}