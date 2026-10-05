using Haven.Application.Common;
using Haven.Application.Common.Messaging;
using Haven.Application.Configuration;
using Haven.Application.Features.CustomActions.Abstractions;
using Haven.Application.Mappers;
using Haven.Domain.Entities;

using Microsoft.Extensions.Options;

namespace Haven.Application.Features.CustomActions.Queries.GetCustomAction;

public sealed class GetCustomActionHandler(ICustomActionRepository repository,
    IOptionsMonitor<NetworkOptions> networkOptions)
    : IQueryHandler<GetCustomActionQuery, CustomActionDto>
{
    public async ValueTask<Result<CustomActionDto>> Handle(GetCustomActionQuery query, CancellationToken cancellationToken)
    {
        var action = await repository.GetByIdAsync(query.ActionId, cancellationToken);
        if (action is null || action.ServiceId != query.ServiceId)
            return Error.NotFoundFor(nameof(CustomAction), query.ActionId);

        var dto = action.ToDto();

        dto.WebhookUrl = networkOptions.CurrentValue.BuildEndpointRoute($"/webhooks/action/{action.Token}");

        return Result<CustomActionDto>.Success(dto);
    }
}