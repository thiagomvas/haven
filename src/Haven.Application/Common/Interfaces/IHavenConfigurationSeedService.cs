namespace Haven.Application.Common.Interfaces;

using Configuration;

public interface IHavenConfigurationSeedService
{
    Task SeedAsync(CancellationToken ct = default);

    /// <summary>
    /// Upserts every section of <paramref name="config"/> into the settings store directly,
    /// without reading from the <c>haven.yml</c> file. Used when applying configuration that may
    /// itself change where that file lives (e.g. <see cref="ManifestsOptions.ManifestsPath"/>),
    /// so the new values take effect before anything tries to locate the file at its new path.
    /// </summary>
    Task SeedFromAsync(HavenConfiguration config, CancellationToken ct = default);
}