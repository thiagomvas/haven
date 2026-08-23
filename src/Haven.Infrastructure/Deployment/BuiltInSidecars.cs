using Haven.Domain.Enums;
using Haven.Domain.ValueObjects;

namespace Haven.Infrastructure.Deployment;

/// <summary>
/// The fixed catalog of sidecars Haven ships with. Seeded into the database on startup
/// (see <c>HavenBootstrapper.SeedBuiltInSidecarsAsync</c>) so users can only enable/disable them,
/// not create arbitrary ones, for now.
/// </summary>
public sealed record BuiltInSidecarDefinition(string Name, SidecarKind Kind, DockerConfig SourceConfig, bool DevelopmentOnly);

public static class BuiltInSidecars
{
    public static readonly IReadOnlyList<BuiltInSidecarDefinition> All =
    [
        new BuiltInSidecarDefinition(
            "traefik",
            SidecarKind.Traefik,
            new DockerConfig { Image = "traefik:v3.0", Ports = [ "80:80" ], RestartPolicy = RestartPolicy.Always },
            DevelopmentOnly: false),

        new BuiltInSidecarDefinition(
            "whoami",
            SidecarKind.Whoami,
            new DockerConfig { Image = "traefik/whoami", Ports = [], RestartPolicy = RestartPolicy.Always },
            DevelopmentOnly: true)
    ];
}