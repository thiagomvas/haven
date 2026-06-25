using System.Text.Json;

using Haven.Application.Common;
using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Messaging;
using Haven.Application.Configuration;
using Haven.Application.Features.Configuration.Events;

namespace Haven.Application.Features.Configuration.Commands.UpdateTelemetry;

public sealed class UpdateTelemetryHandler(
    IHavenSettingRepository repository,
    IHavenConfigurationStore store,
    IUnitOfWork unitOfWork,
    Mediator.IMediator mediator)
    : ICommandHandler<UpdateTelemetryCommand, TelemetryOptions>
{
    public async ValueTask<Result<TelemetryOptions>> Handle(UpdateTelemetryCommand request, CancellationToken ct)
    {
        await repository.UpsertAsync(TelemetryOptions.SectionName, JsonSerializer.Serialize(request.Options), ct);
        unitOfWork.OnAfterSave(() => store.Invalidate(TelemetryOptions.SectionName));

        await mediator.Publish(new ConfigurationUpdatedNotification(), ct);

        return Result<TelemetryOptions>.Success(request.Options);
    }
}