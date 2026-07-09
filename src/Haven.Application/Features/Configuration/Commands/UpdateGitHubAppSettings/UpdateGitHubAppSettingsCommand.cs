using Haven.Application.Common;
using Haven.Application.Common.Messaging;
using Haven.Application.Features.Configuration.Dtos;
using Haven.Domain;

namespace Haven.Application.Features.Configuration.Commands.UpdateGitHubAppSettings;

[AdminOnly]
public sealed class UpdateGitHubAppSettingsCommand : ICommand<GitHubAppSettingsDto>
{
    public string ClientId { get; set; } = string.Empty;
    public Optional<string> ClientSecret { get; set; }
}
