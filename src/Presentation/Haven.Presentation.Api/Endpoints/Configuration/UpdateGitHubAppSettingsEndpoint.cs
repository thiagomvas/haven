using FastEndpoints;

using Haven.Application.Common.Responses;
using Haven.Application.Features.Configuration.Commands.UpdateGitHubAppSettings;
using Haven.Application.Features.Configuration.Dtos;
using Haven.Presentation.Api.Extensions;

using Mediator;

namespace Haven.Presentation.Api.Endpoints.Configuration;

public sealed class UpdateGitHubAppSettingsEndpoint(IMediator mediator)
    : Endpoint<UpdateGitHubAppSettingsCommand, ApiResponse<GitHubAppSettingsDto>>
{
    public override void Configure()
    {
        Put("/configuration/github-app");
        Options(x => x.WithTags("Configuration"));
        Summary(s =>
        {
            s.Summary = "Update GitHub App OAuth configuration";
            s[200] = "GitHub App configuration updated";
            s[400] = "Validation error";
        });
    }

    public override async Task HandleAsync(UpdateGitHubAppSettingsCommand req, CancellationToken ct)
    {
        var result = await mediator.Send(req, ct);
        await this.SendResultAsync(result, ct);
    }
}