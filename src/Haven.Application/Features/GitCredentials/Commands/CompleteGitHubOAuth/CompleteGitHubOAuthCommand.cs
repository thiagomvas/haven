using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.GitCredentials.Commands.CompleteGitHubOAuth;

public sealed class CompleteGitHubOAuthCommand : ICommand<Guid>
{
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// When set, this reconnect rotates the existing credential's tokens instead of creating a new one.
    /// </summary>
    public Guid? CredentialId { get; set; }
}