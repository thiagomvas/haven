using FastEndpoints;
using Haven.Application.Common.Responses;
using Haven.Application.Features.HelloWorld;
using Mediator;

namespace Haven.Presentation.Api.Endpoints.HelloWorld;

public class GetHelloWorldEndpoint : EndpointWithoutRequest<ApiResponse<string>>
{
    private readonly IMediator _mediator;

    public GetHelloWorldEndpoint(IMediator mediator) => _mediator = mediator;

    public override void Configure()
    {
        Get("/hello-world");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetHelloWorldQuery(), ct);
        var response = ApiResponse<string>.FromResult(result);

        if (result.IsSuccess)
            await Send.OkAsync(ct);
        else
            await Send.ResponseAsync(response, StatusCodes.Status400BadRequest, ct);
    }
}
