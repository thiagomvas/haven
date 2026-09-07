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

    /// <summary>
    /// Extracts the resolver name from the first configured <c>--certificatesresolvers.&lt;name&gt;.acme.*</c>
    /// flag (e.g. "letsencrypt" out of "--certificatesresolvers.letsencrypt.acme.email=..."), so the
    /// <c>tls.certresolver</c> label always matches whatever name the ACME resolver was actually
    /// registered under, rather than assuming the built-in quick-setup's default name.
    /// </summary>
    public string? GetAcmeResolverName()
    {
        const string prefix = "--certificatesresolvers.";
        foreach (var arg in CommandArgs)
        {
            if (!arg.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                continue;

            var rest = arg[prefix.Length..];
            var dotIndex = rest.IndexOf('.');
            if (dotIndex <= 0)
                continue;

            var name = rest[..dotIndex];
            if (rest[(dotIndex + 1)..].StartsWith("acme.", StringComparison.OrdinalIgnoreCase))
                return name;
        }

        return null;
    }
}