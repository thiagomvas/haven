using System.Text.Json;

using Haven.Application.Common;
using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Messaging;
using Haven.Application.Configuration;
using Haven.Application.Features.Configuration.Dtos;
using Haven.Application.Features.Configuration.Events;

using Microsoft.Extensions.Options;

namespace Haven.Application.Features.Configuration.Commands.UpdateGitHubAppSettings;

public sealed class UpdateGitHubAppSettingsHandler(
    IHavenSettingRepository repository,
    IHavenConfigurationStore store,
    IEncryptionService encryptionService,
    IOptionsMonitor<NetworkOptions> networkOptions,
    IUnitOfWork unitOfWork,
    Mediator.IMediator mediator)
    : ICommandHandler<UpdateGitHubAppSettingsCommand, GitHubAppSettingsDto>
{
    public async ValueTask<Result<GitHubAppSettingsDto>> Handle(UpdateGitHubAppSettingsCommand command, CancellationToken ct)
    {
        var existingJson = await repository.GetAsync(GitHubAppOptions.SectionName, ct);
        var existing = existingJson is null
            ? new GitHubAppOptions()
            : JsonSerializer.Deserialize<GitHubAppOptions>(existingJson) ?? new GitHubAppOptions();

        var options = new GitHubAppOptions
        {
            ClientId = command.ClientId,
            ClientSecret = command.ClientSecret.HasValue
                ? encryptionService.Encrypt(command.ClientSecret.Value)
                : existing.ClientSecret,
        };

        await repository.UpsertAsync(GitHubAppOptions.SectionName, JsonSerializer.Serialize(options), ct);
        unitOfWork.OnAfterSave(() => store.Invalidate(GitHubAppOptions.SectionName));

        await mediator.Publish(new ConfigurationUpdatedNotification(), ct);

        var host = networkOptions.CurrentValue.BuildHost();
        var isConfigured = !string.IsNullOrEmpty(options.ClientId) && !string.IsNullOrEmpty(options.ClientSecret) && host is not null;
        var redirectUri = host is not null ? $"{host}{GitHubAppOptions.CallbackPath}" : string.Empty;
        var dto = new GitHubAppSettingsDto(options.ClientId, redirectUri, isConfigured);
        return Result<GitHubAppSettingsDto>.Success(dto);
    }
}
