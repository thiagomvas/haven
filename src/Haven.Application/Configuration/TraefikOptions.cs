namespace Haven.Application.Configuration;

public class TraefikOptions
{
    public const string SectionName = "Traefik";

    /// <summary>
    /// Root directory on the host where Haven writes Traefik's file-provider dynamic config
    /// (per-domain custom TLS certificates, plus the internal-API router). Bind-mounted read-only
    /// from Traefik's perspective into <c>/etc/traefik/dynamic</c> at deploy time. Must be a path
    /// the Docker daemon can bind-mount.
    /// </summary>
    public string DynamicConfigRootPath { get; set; } = "/data/traefik/dynamic";

    /// <summary>
    /// HTTP Basic Auth credentials gating the Traefik dashboard router (see
    /// <c>DockerUtils.BuildTraefikDashboardLabels</c>). Null/null means auth is disabled. Stored
    /// here - not on the <c>Sidecar</c> aggregate - because this is Traefik-specific configuration,
    /// not a generic sidecar concept other kinds would ever populate.
    /// </summary>
    public string? DashboardAuthUsername { get; set; }

    /// <summary>BCrypt hash (htpasswd-compatible) of the dashboard password. Never the plaintext.</summary>
    public string? DashboardAuthPasswordHash { get; set; }
}