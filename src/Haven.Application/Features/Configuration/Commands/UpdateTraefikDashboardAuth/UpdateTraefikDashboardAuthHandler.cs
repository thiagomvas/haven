using System.Text.Json;

using Haven.Application.Common;
using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Interfaces.Services;
using Haven.Application.Common.Messaging;
using Haven.Application.Configuration;
using Haven.Application.Features.Configuration.Events;

using Microsoft.Extensions.Options;

namespace Haven.Application.Features.Configuration.Commands.UpdateTraefikDashboardAuth;

public sealed class UpdateTraefikDashboardAuthHandler(
    IOptionsMonitor<TraefikOptions> currentOptions,
    IHavenSettingRepository repository,
    IHavenConfigurationStore store,
    IPasswordHasher passwordHasher,
    IUnitOfWork unitOfWork,
    Mediator.IMediator mediator)
    : ICommandHandler<UpdateTraefikDashboardAuthCommand, TraefikDashboardAuthDto>
{
    public async ValueTask<Result<TraefikDashboardAuthDto>> Handle(UpdateTraefikDashboardAuthCommand request, CancellationToken ct)
    {
        var options = currentOptions.CurrentValue;

        if (!request.Enabled)
        {
            options.DashboardAuthUsername = null;
            options.DashboardAuthPasswordHash = null;
        }
        else
        {
            options.DashboardAuthUsername = request.Username;
            if (!string.IsNullOrEmpty(request.Password))
                options.DashboardAuthPasswordHash = passwordHasher.Hash(request.Password);
        }

        await repository.UpsertAsync(TraefikOptions.SectionName, JsonSerializer.Serialize(options), ct);
        unitOfWork.OnAfterSave(() => store.Invalidate(TraefikOptions.SectionName));

        await mediator.Publish(new ConfigurationUpdatedNotification(), ct);

        return Result<TraefikDashboardAuthDto>.Success(new TraefikDashboardAuthDto
        {
            Enabled = options.DashboardAuthPasswordHash is not null,
            Username = options.DashboardAuthUsername
        });
    }
}
