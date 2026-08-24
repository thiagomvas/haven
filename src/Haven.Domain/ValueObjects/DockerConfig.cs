using Haven.Domain.Enums;

namespace Haven.Domain.ValueObjects;

public sealed class DockerConfig : ServiceSourceConfig
{
    public string Image { get; set; } = string.Empty;
    public List<string> Ports { get; set; } = [];
    public List<string> CommandArgs { get; set; } = [];
    public RestartPolicy RestartPolicy { get; set; } = RestartPolicy.UnlessStopped;

    /// <summary>
    /// Whether these command args configure a Traefik ACME certificate resolver (e.g. via the
    /// "SSL" quick-setup toggle, or a hand-rolled <c>--certificatesresolvers.*.acme.*</c> flag).
    /// This is the single source of truth for "is ACME configured" on a Traefik sidecar — consumed
    /// both by deploy-time logic (e.g. whether to mount the ACME storage volume) and by the
    /// per-domain TLS guard-rail warning, so the check is never duplicated.
    /// </summary>
    public bool HasAcmeResolverConfigured() =>
        CommandArgs.Any(a =>
            a.StartsWith("--certificatesresolvers.", StringComparison.OrdinalIgnoreCase) && a.Contains(".acme."));
}