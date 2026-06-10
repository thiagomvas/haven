using System.Text.Json;

using Haven.Application.Common;
using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Messaging;
using Haven.Application.Configuration;
using Haven.Application.Features.Configuration.Events;

namespace Haven.Application.Features.Backups.Commands.UpdateBackupOptions;

public sealed class UpdateBackupOptionsHandler(
    IHavenSettingRepository repository,
    IHavenConfigurationStore store,
    Mediator.IMediator mediator)
    : ICommandHandler<UpdateBackupOptionsCommand, BackupOptions>
{
    public async ValueTask<Result<BackupOptions>> Handle(UpdateBackupOptionsCommand request, CancellationToken ct)
    {
        await repository.UpsertAsync(BackupOptions.SectionName, JsonSerializer.Serialize(request.Options), ct);
        store.Invalidate(BackupOptions.SectionName);

        await mediator.Publish(new ConfigurationUpdatedNotification(), ct);

        return Result<BackupOptions>.Success(request.Options);
    }
}
