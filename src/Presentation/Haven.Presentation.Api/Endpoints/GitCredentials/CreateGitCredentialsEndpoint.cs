using FastEndpoints;

using Haven.Application.Common.Responses;
using Haven.Application.Features.GitCredentials.Commands.CreateGitCredentials;
using Haven.Presentation.Api.Extensions;

using Mediator;

namespace Haven.Presentation.Api.Endpoints.GitCredentials;

public class CreateGitCredentialsEndpoint : Endpoint<CreateGitCredentialsCommand, ApiResponse<Guid>>
{
    private readonly IMediator _mediator;

    public CreateGitCredentialsEndpoint(IMediator mediator) => _mediator = mediator;

    public override void Configure()
    {
        Post("/credentials");
        
        Options(x => x.WithTags("Git Credentials"));
        Summary(s =>
        {
            s.Summary = "Create git credentials";
            s.Description = "Creates new git credentials for authenticating with git providers.";
            s[201] = "Created";
            s[400] = "Validation error";
            s[409] = "Credentials with this display name already exist";
        });
    }

    public override async Task HandleAsync(CreateGitCredentialsCommand req, CancellationToken ct)
    {
        var result = await _mediator.Send(req, ct);
        await this.SendResultAsync(result, ct);
    }
}