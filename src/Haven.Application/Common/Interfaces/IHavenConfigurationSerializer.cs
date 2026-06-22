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
}