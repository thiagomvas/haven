using FastEndpoints;

using Haven.Application.Common.Messaging;
using Haven.Application.Common.Responses;
using Haven.Application.Features.GitCredentials;
using Haven.Application.Features.GitCredentials.Queries.GetGitCredentials;
using Haven.Presentation.Api.Extensions;

using Mediator;

namespace Haven.Presentation.Api.Endpoints.GitCredentials;

public class GetGitCredentialsPagedEndpoint(IMediator mediator) : Endpoint<GetGitCredentialsPagedQuery, PagedResult<GitCredentialDto>>
{
    public override void Configure()
    {
        Get("/credentials");
        AllowAnonymous();
        Options(x => x.WithTags("Git Credentials"));
        Summary(s =>
        {
            s.Summary = "Get git credentials";
            s.Description = "Gets all git credentials paged.";
        });
    }

    public override async Task HandleAsync(GetGitCredentialsPagedQuery req, CancellationToken ct)
    {
        var result = await mediator.Send(req, ct);
        await this.SendResultAsync(result, ct);
    }
}