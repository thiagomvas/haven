using Haven.Application.Common;
using Haven.Application.Common.Messaging;
using Haven.Domain;
using Haven.Domain.Enums;

namespace Haven.Application.Features.GitCredentials.Commands.CreateGitCredentials;

[RequirePermission(Permissions.System.ManageGitCredentials)]
public sealed class CreateGitCredentialsCommand : ICommand<Guid>
{
    public GitProviderType ProviderType { get; set; } = GitProviderType.Generic;
    public string? HostUrl { get; set; }
    public GitAuthMethod AuthMethod { get; set; } = GitAuthMethod.Token;
    public string PrimaryCredential { get; set; } = string.Empty;
    public string? SecondaryCredential { get; set; }
    public string? WebhookSecret { get; set; }
    public string DisplayName { get; set; } = string.Empty;
}