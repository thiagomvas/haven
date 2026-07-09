namespace Haven.Application.Common.Interfaces.Deployment;

/// <summary>
/// Translates a path inside Haven's own filesystem view to the equivalent path on the Docker
/// host. Needed because Haven typically runs Docker-outside-of-Docker (it talks to the host's
/// Docker daemon over a bind-mounted socket), so a directory Haven creates locally is only a
/// valid bind-mount source for containers *the host daemon creates* if translated to the host's
/// real path first.
/// </summary>
public interface IHostPathResolver
{
    /// <summary>
    /// Resolves <paramref name="containerLocalPath"/> (a path as seen inside Haven's own
    /// container) to the corresponding path on the Docker host. Returns the input unchanged
    /// when Haven isn't running in a container, or no matching mount is found.
    /// </summary>
    Task<string> ResolveAsync(string containerLocalPath, CancellationToken cancellationToken);
}
