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
}

public sealed class TraefikRouterInfo
{
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public bool HasTls { get; set; }
    public List<string> Errors { get; set; } = [];
}
