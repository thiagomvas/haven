namespace Haven.Application.Common.Interfaces;

using Configuration;

public interface IHavenConfigurationSerializer
{
    bool FileExists();
    Task<HavenConfiguration> ReadAsync(CancellationToken ct);
    Task WriteAsync(HavenConfiguration config, CancellationToken ct);
    Task<string> ReadRawAsync(CancellationToken ct);
    Task WriteRawAsync(string yaml, CancellationToken ct);
    bool TryParse(string yaml, out string? error);

    /// <summary>
    /// Deserializes <paramref name="yaml"/> without touching the filesystem. Callers should
    /// validate with <see cref="TryParse"/> first.
    /// </summary>
    HavenConfiguration Parse(string yaml);
}