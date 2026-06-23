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
        if (!serializer.FileExists())
        {
            logger.LogInformation("Configuration file not found, writing current database configuration to file");
            var dbConfig = new HavenConfiguration
            {
                Manifests = store.GetCurrentValue<ManifestsOptions>(ManifestsOptions.SectionName),
                Instance = store.GetCurrentValue<InstanceOptions>(InstanceOptions.SectionName),
                Network = store.GetCurrentValue<NetworkOptions>(NetworkOptions.SectionName),
                Backup = store.GetCurrentValue<BackupOptions>(BackupOptions.SectionName),
                Telemetry = store.GetCurrentValue<TelemetryOptions>(TelemetryOptions.SectionName)
            };
            await serializer.WriteAsync(dbConfig, ct);
            return;
        }

        logger.LogInformation("Seeding Haven configuration from haven.yml into database");

        var config = await serializer.ReadAsync(ct);

        await UpsertAndInvalidateAsync(ManifestsOptions.SectionName, config.Manifests, ct);
        await UpsertAndInvalidateAsync(InstanceOptions.SectionName, config.Instance, ct);
        await UpsertAndInvalidateAsync(NetworkOptions.SectionName, config.Network, ct);
        await UpsertAndInvalidateAsync(BackupOptions.SectionName, config.Backup, ct);
        await UpsertAndInvalidateAsync(TelemetryOptions.SectionName, config.Telemetry, ct);

        logger.LogInformation("Haven configuration seeded successfully");
    }

    private async Task UpsertAndInvalidateAsync<T>(string sectionName, T value, CancellationToken ct)
    {
        await repository.UpsertAsync(sectionName, JsonSerializer.Serialize(value), ct);
        store.Invalidate(sectionName);
    }
}