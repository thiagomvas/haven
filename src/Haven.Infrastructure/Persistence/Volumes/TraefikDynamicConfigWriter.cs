using Haven.Application.Common;
using Haven.Application.Common.Interfaces.Services;
using Haven.Application.Configuration;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Haven.Infrastructure.Persistence.Volumes;

/// <summary>
/// Writes one subdirectory per domain (<c>{root}/{domainId}/</c>) rather than a single shared
/// dynamic-config file, since Traefik's file provider merges every file under the watched
/// directory — per-domain files avoid read-modify-write races across concurrent cert updates for
/// different domains.
/// </summary>
public sealed class TraefikDynamicConfigWriter(
    IOptionsMonitor<TraefikOptions> options,
    ILogger<TraefikDynamicConfigWriter> logger) : ITraefikDynamicConfigWriter
{
    private const string InternalRouterDirName = "_haven-internal";

    public async Task<Result> WriteDomainCertificateAsync(Guid domainId, string certificatePem, string privateKeyPem, CancellationToken ct = default)
    {
        var domainDir = DomainDir(domainId);
        Directory.CreateDirectory(domainDir);

        var certPath = Path.Combine(domainDir, "cert.pem");
        var keyPath = Path.Combine(domainDir, "key.pem");
        var configPath = Path.Combine(domainDir, "config.yml");

        await File.WriteAllTextAsync(certPath, certificatePem, ct);
        await File.WriteAllTextAsync(keyPath, privateKeyPem, ct);
        TrySetOwnerOnlyPermissions(keyPath);
        TrySetOwnerOnlyPermissions(domainDir);

        var yaml = $"""
                    tls:
                      certificates:
                        - certFile: /etc/traefik/dynamic/{domainId}/cert.pem
                          keyFile: /etc/traefik/dynamic/{domainId}/key.pem
                    """;
        await File.WriteAllTextAsync(configPath, yaml, ct);

        logger.LogDebug("Wrote Traefik dynamic TLS config for domain {DomainId}", domainId);
        return Result.Success();
    }

    public Task<Result> RemoveDomainCertificateAsync(Guid domainId, CancellationToken ct = default)
    {
        var domainDir = DomainDir(domainId);
        if (Directory.Exists(domainDir))
        {
            Directory.Delete(domainDir, recursive: true);
            logger.LogDebug("Removed Traefik dynamic TLS config for domain {DomainId}", domainId);
        }

        return Task.FromResult(Result.Success());
    }

    public async Task<Result> WriteInternalApiRouterAsync(CancellationToken ct = default)
    {
        var dir = Path.Combine(Root, InternalRouterDirName);
        Directory.CreateDirectory(dir);

        var configPath = Path.Combine(dir, "config.yml");
        var yaml = """
                   http:
                     routers:
                       haven-internal-api:
                         rule: "PathPrefix(`/api`)"
                         entrypoints: ["havenapi"]
                         service: "api@internal"
                   """;
        await File.WriteAllTextAsync(configPath, yaml, ct);

        return Result.Success();
    }

    private string Root => Path.GetFullPath(options.CurrentValue.DynamicConfigRootPath);

    private string DomainDir(Guid domainId) => Path.Combine(Root, domainId.ToString());

    /// <summary>
    /// Best-effort — restricts read access to the process owner on platforms where Haven runs
    /// as a distinct user (POSIX). No-op on platforms/filesystems where this isn't supported.
    /// </summary>
    private static void TrySetOwnerOnlyPermissions(string path)
    {
        try
        {
            if (File.Exists(path))
                File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite);
            else if (Directory.Exists(path))
                File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        }
        catch
        {
            // Not supported on this platform/filesystem - the mount is still only reachable by
            // Traefik and Haven's own container, so this is defense in depth, not the only guard.
        }
    }
}
