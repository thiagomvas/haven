using Haven.Application.Common;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Messaging;
using Haven.Application.Configuration;
using Haven.Application.Features.CustomActions.Abstractions;
using Haven.Application.Mappers;
using Haven.Domain.Aggregates;

using Microsoft.Extensions.Options;

namespace Haven.Application.Features.CustomActions.Queries.ListCustomActionsForService;

public sealed class ListCustomActionsForServiceHandler(
    IServiceRepository serviceRepository,
    ICustomActionRepository repository,
    IOptionsMonitor<NetworkOptions> networkOptions)
    : IQueryHandler<ListCustomActionsForServiceQuery, IReadOnlyList<CustomActionDto>>
{
    public async ValueTask<Result<IReadOnlyList<CustomActionDto>>> Handle(
        ListCustomActionsForServiceQuery query, CancellationToken cancellationToken)
    {
        var service = await serviceRepository.GetByIdAsync(query.ServiceId, cancellationToken);
        if (service is null)
            return Error.NotFoundFor(nameof(Service), query.ServiceId);

        var actions = await repository.GetForServiceAsync(query.ServiceId, cancellationToken);
        var options = networkOptions.CurrentValue;
        IReadOnlyList<CustomActionDto> dtos = actions.Select(action =>
        {
            var dto = action.ToDto();
            dto.WebhookUrl = options.BuildEndpointRoute($"/webhooks/action/{action.Token}");
            return dto;
        }).ToList();

        return Result<IReadOnlyList<CustomActionDto>>.Success(dtos);
    }
}
