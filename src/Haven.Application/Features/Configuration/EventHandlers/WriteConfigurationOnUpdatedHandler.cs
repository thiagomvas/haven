using Haven.Application.Common.Interfaces;
using Haven.Application.Configuration;
using Haven.Application.Features.Configuration.Events;

using Mediator;

using Microsoft.Extensions.Options;

namespace Haven.Application.Features.Configuration.EventHandlers;

public sealed class WriteConfigurationOnUpdatedHandler(
    IHavenConfigurationSerializer serializer,
    IOptionsMonitor<ManifestsOptions> manifests,
    IOptionsMonitor<InstanceOptions> instance,
    IOptionsMonitor<NetworkOptions> network,
    IOptionsMonitor<BackupOptions> backup)
    : INotificationHandler<ConfigurationUpdatedNotification>
{
    public async ValueTask Handle(ConfigurationUpdatedNotification notification, CancellationToken cancellationToken)
    {
        var config = new HavenConfiguration
        {
            Manifests = manifests.CurrentValue,
            Instance = instance.CurrentValue,
            Network = network.CurrentValue,
            Backup = backup.CurrentValue,
        };

        await serializer.WriteAsync(config, cancellationToken);
    }
}
