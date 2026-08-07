using Haven.Application.Common;
using Haven.Application.Common.Messaging;
using Haven.Domain;
using Haven.Domain.Enums;

namespace Haven.Application.Features.GitCredentials.Commands.RotateGitCredentials;

[RequirePermission(Permissions.System.ManageGitCredentials)]
public sealed class RotateGitCredentialsCommand : ICommand<Guid>
{
    public Guid Id { get; set; }
    public GitAuthMethod AuthMethod { get; set; } = GitAuthMethod.Token;
    public string PrimaryCredential { get; set; } = string.Empty;
    public string? SecondaryCredential { get; set; }
    public string? WebhookSecret { get; set; }
}