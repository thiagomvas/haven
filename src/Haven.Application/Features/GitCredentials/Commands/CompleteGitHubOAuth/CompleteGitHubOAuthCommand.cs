using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.GitCredentials.Commands.CompleteGitHubOAuth;

public sealed class CompleteGitHubOAuthCommand : ICommand<Guid>
{
    public string Code { get; set; } = string.Empty;
}
