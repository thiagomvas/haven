using Haven.Application.Common;
using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.GitCredentials.Queries.StartGitHubOAuth;

[RequirePermission(Permissions.System.ManageGitCredentials)]
public sealed class StartGitHubOAuthQuery : IQuery<string>;
