using System.Text.Json;

using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Configuration;

using Microsoft.Extensions.Logging;

namespace Haven.Infrastructure.Configuration;

public sealed class HavenConfigurationSeedService(
    IHavenConfigurationSerializer serializer,
    IHavenSettingRepository repository,
    IHavenConfigurationStore store,
    ILogger<HavenConfigurationSeedService> logger) : IHavenConfigurationSeedService
{
    public async Task SeedAsync(CancellationToken ct = default)
    {
        logger.LogInformation("Seeding Haven configuration from haven.yml into database");

        var config = await serializer.ReadAsync(ct);

        await UpsertAndInvalidateAsync(ManifestsOptions.SectionName, config.Manifests, ct);
        await UpsertAndInvalidateAsync(InstanceOptions.SectionName, config.Instance, ct);
        await UpsertAndInvalidateAsync(NetworkOptions.SectionName, config.Network, ct);
        await UpsertAndInvalidateAsync(BackupOptions.SectionName, config.Backup, ct);

        logger.LogInformation("Haven configuration seeded successfully");
    }

    private async Task UpsertAndInvalidateAsync<T>(string sectionName, T value, CancellationToken ct)
    {
        await repository.UpsertAsync(sectionName, JsonSerializer.Serialize(value), ct);
        store.Invalidate(sectionName);
    }
}
