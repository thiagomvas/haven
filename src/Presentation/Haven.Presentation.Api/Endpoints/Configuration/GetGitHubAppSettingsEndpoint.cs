using FastEndpoints;

using Haven.Application.Common.Responses;
using Haven.Application.Features.Configuration.Dtos;
using Haven.Application.Features.Configuration.Queries.GetGitHubAppSettings;
using Haven.Presentation.Api.Extensions;

using Mediator;

namespace Haven.Presentation.Api.Endpoints.Configuration;

public sealed class GetGitHubAppSettingsEndpoint(IMediator mediator)
    : Endpoint<EmptyRequest, ApiResponse<GitHubAppSettingsDto>>
{
    public override void Configure()
    {
        Get("/configuration/github-app");
        Options(x => x.WithTags("Configuration"));
        Summary(s =>
        {
            s.Summary = "Get GitHub App OAuth configuration";
            s[200] = "GitHub App configuration retrieved";
        });
    }

    public override async Task HandleAsync(EmptyRequest req, CancellationToken ct)
    {
        var result = await mediator.Send(new GetGitHubAppSettingsQuery(), ct);
        await this.SendResultAsync(result, ct);
    }
}
