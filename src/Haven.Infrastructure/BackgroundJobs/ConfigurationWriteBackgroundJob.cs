using Haven.Application.Common.Interfaces;
using Haven.Application.Configuration;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Haven.Infrastructure.BackgroundJobs;

public sealed class ConfigurationWriteBackgroundJob(
    IHavenConfigurationStore store,
    IHavenConfigurationSerializer serializer,
    IOptionsMonitor<ManifestsOptions> manifests,
    IOptionsMonitor<InstanceOptions> instance,
    IOptionsMonitor<NetworkOptions> network,
    IOptionsMonitor<BackupOptions> backup,
    IOptionsMonitor<TelemetryOptions> telemetry,
    IOptionsMonitor<VolumesOptions> volumes,
    IOptionsMonitor<DockerCleanupOptions> dockerCleanup,
    ILogger<ConfigurationWriteBackgroundJob> logger)
{
    public async Task ExecuteAsync()
    {
        logger.LogInformation("Writing configuration to disk");

        var config = new HavenConfiguration
        {
            Manifests = manifests.CurrentValue,
            Instance = instance.CurrentValue,
            Network = network.CurrentValue,
            Backup = backup.CurrentValue,
            Telemetry = telemetry.CurrentValue,
            Volumes = volumes.CurrentValue,
            DockerCleanup = dockerCleanup.CurrentValue
        };

        await serializer.WriteAsync(config, CancellationToken.None);
    }
}