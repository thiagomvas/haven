namespace Haven.Application.Common.Interfaces;

public interface IHavenConfigurationSeedService
{
    Task SeedAsync(CancellationToken ct = default);
}
