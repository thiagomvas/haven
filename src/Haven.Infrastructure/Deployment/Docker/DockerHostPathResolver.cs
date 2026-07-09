using Docker.DotNet;
using Docker.DotNet.Models;

using Haven.Application.Common.Interfaces.Deployment;

using Microsoft.Extensions.Logging;

namespace Haven.Infrastructure.Deployment;

/// <inheritdoc cref="IHostPathResolver"/>
public class DockerHostPathResolver : IHostPathResolver
{
    private readonly IDockerClient _dockerClient;
    private readonly ILogger<DockerHostPathResolver> _logger;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private IList<MountPoint>? _selfMounts;
    private bool _resolved;

    public DockerHostPathResolver(IDockerClient dockerClient, ILogger<DockerHostPathResolver> logger)
    {
        _dockerClient = dockerClient;
        _logger = logger;
    }

    public async Task<string> ResolveAsync(string containerLocalPath, CancellationToken cancellationToken)
    {
        var mounts = await GetSelfMountsAsync(cancellationToken);
        if (mounts is null || mounts.Count == 0)
            return containerLocalPath;

        var normalized = containerLocalPath.Replace('\\', '/').TrimEnd('/');

        MountPoint? best = null;
        foreach (var mount in mounts)
        {
            if (string.IsNullOrEmpty(mount.Destination)) continue;
            var dest = mount.Destination.TrimEnd('/');

            var isMatch = normalized == dest || normalized.StartsWith(dest + "/", StringComparison.Ordinal);
            if (!isMatch) continue;

            if (best is null || dest.Length > best.Destination.TrimEnd('/').Length)
                best = mount;
        }

        if (best is null)
            return containerLocalPath;

        var bestDest = best.Destination.TrimEnd('/');
        var relative = normalized.Length > bestDest.Length ? normalized[bestDest.Length..].TrimStart('/') : string.Empty;
        var hostSource = best.Source.TrimEnd('/');

        return string.IsNullOrEmpty(relative) ? hostSource : $"{hostSource}/{relative}";
    }

    private async Task<IList<MountPoint>?> GetSelfMountsAsync(CancellationToken cancellationToken)
    {
        if (_resolved) return _selfMounts;

        await _lock.WaitAsync(cancellationToken);
        try
        {
            if (_resolved) return _selfMounts;

            var containerId = System.Environment.GetEnvironmentVariable("HOSTNAME");
            if (string.IsNullOrWhiteSpace(containerId))
            {
                _logger.LogDebug("No HOSTNAME environment variable found; assuming Haven is not running in a container, skipping host path translation");
                _resolved = true;
                return _selfMounts;
            }

            try
            {
                var inspect = await _dockerClient.Containers.InspectContainerAsync(containerId, cancellationToken);
                _selfMounts = inspect.Mounts;
            }
            catch (DockerApiException ex)
            {
                _logger.LogDebug(ex,
                    "Could not inspect own container '{ContainerId}' to resolve host paths; assuming Haven is not running in a container",
                    containerId);
                _selfMounts = null;
            }

            _resolved = true;
            return _selfMounts;
        }
        finally
        {
            _lock.Release();
        }
    }
}