using Haven.Application.Common;
using Haven.Application.Common.Messaging;
using Haven.Application.Configuration;
using Haven.Application.Features.Configuration.Dtos;

using Microsoft.Extensions.Options;

namespace Haven.Application.Features.Configuration.Queries.GetGitHubAppSettings;

public sealed class GetGitHubAppSettingsHandler(
    IOptionsMonitor<GitHubAppOptions> options,
    IOptionsMonitor<NetworkOptions> networkOptions)
    : IQueryHandler<GetGitHubAppSettingsQuery, GitHubAppSettingsDto>
{
    public ValueTask<Result<GitHubAppSettingsDto>> Handle(GetGitHubAppSettingsQuery request, CancellationToken ct)
    {
        var opts = options.CurrentValue;
        var host = networkOptions.CurrentValue.BuildHost();
        var isConfigured = !string.IsNullOrEmpty(opts.ClientId) && !string.IsNullOrEmpty(opts.ClientSecret) && host is not null;
        var redirectUri = host is not null ? $"{host}{GitHubAppOptions.CallbackPath}" : string.Empty;
        var dto = new GitHubAppSettingsDto(opts.ClientId, redirectUri, isConfigured);
        return ValueTask.FromResult(Result<GitHubAppSettingsDto>.Success(dto));
    }
}