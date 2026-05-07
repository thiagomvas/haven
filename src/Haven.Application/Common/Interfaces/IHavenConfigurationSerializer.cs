namespace Haven.Application.Common.Interfaces;

using Configuration;

public interface IHavenConfigurationSerializer
{
    Task<HavenConfiguration> ReadAsync(CancellationToken ct);
    Task WriteAsync(HavenConfiguration config, CancellationToken ct);
}
