using Haven.Application.Common;
using Haven.Application.Common.Messaging;
using Haven.Application.Features.Configuration.Dtos;

namespace Haven.Application.Features.Configuration.Queries.GetGitHubAppSettings;

[RequirePermission(Permissions.System.ManageGitCredentials)]
public sealed record GetGitHubAppSettingsQuery : IQuery<GitHubAppSettingsDto>;