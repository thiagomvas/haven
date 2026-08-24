using Haven.Application.Common;

namespace Haven.Application.Common.Interfaces.Services;

/// <summary>
/// Writes Traefik's file-provider dynamic configuration to the directory bind-mounted into the
/// Traefik sidecar (see <c>TraefikOptions.DynamicConfigRootPath</c>). Traefik watches this
/// directory (<c>--providers.file.watch=true</c>) and picks up changes without a restart.
/// </summary>
public interface ITraefikDynamicConfigWriter
{
    /// <summary>
    /// Writes a domain's custom certificate/key plus the dynamic-config file that registers them
    /// with Traefik's TLS store, so Traefik can match them by SNI for that domain's router.
    /// Overwrites any existing files for the domain (used for both first upload and rotation).
    /// </summary>
    Task<Result> WriteDomainCertificateAsync(Guid domainId, string certificatePem, string privateKeyPem, CancellationToken ct = default);

    /// <summary>
    /// Removes a domain's certificate files and dynamic-config entry (cert removed, TlsMode moved
    /// away from Custom, or the domain itself deleted). No-op if nothing was ever written.
    /// </summary>
    Task<Result> RemoveDomainCertificateAsync(Guid domainId, CancellationToken ct = default);

    /// <summary>
    /// Writes the static dynamic-config file that routes Traefik's internal API
    /// (<c>api@internal</c>) onto the Haven-private <c>havenapi</c> entrypoint. Idempotent -
    /// called once per Traefik sidecar deploy/start.
    /// </summary>
    Task<Result> WriteInternalApiRouterAsync(CancellationToken ct = default);
}
