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
}
