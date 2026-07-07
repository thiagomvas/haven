using Haven.Application.Common;
using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.GitCredentials.Queries.StartGitHubOAuth;

[RequirePermission(Permissions.System.ManageGitCredentials)]
public sealed class StartGitHubOAuthQuery : IQuery<string>
{
    /// <summary>
    /// When set, reconnecting this existing GitHub credential instead of creating a new one.
    /// </summary>
    public Guid? CredentialId { get; set; }
}
