using Haven.Application.Common;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Messaging;
using Haven.Application.Features.Services.ComputedOutputs;

namespace Haven.Application.Features.Services.Queries.GetServiceComputedOutputValue;

public sealed class GetServiceComputedOutputValueHandler(
    IServiceRepository serviceRepository,
    IServiceRegistryEntryRepository serviceRegistryEntryRepository,
    IComputedOutputResolver computedOutputResolver)
    : IQueryHandler<GetServiceComputedOutputValueQuery, ComputedOutputValueDto>
{
    public async ValueTask<Result<ComputedOutputValueDto>> Handle(GetServiceComputedOutputValueQuery query, CancellationToken cancellationToken)
    {
        var service = await serviceRepository.GetByIdAsync(query.ServiceId, cancellationToken);
        if (service is null || service.EnvironmentId != query.EnvironmentId)
            return Error.NotFoundFor("Service", query.ServiceId);

        var property = service.ComputedProperties.FirstOrDefault(p => p.Key == query.Key);
        if (property is null)
            return Error.NotFoundFor("Computed output", query.Key);

        var registry = await serviceRegistryEntryRepository.GetForServiceAsync(query.ServiceId, cancellationToken);
        var resolved = await computedOutputResolver.ResolveAsync(service, registry, cancellationToken);
        var output = resolved.First(o => o.Key == query.Key);

        if (!output.IsAvailable)
            return Error.InvalidOperation($"Output '{query.Key}' is not currently available ({output.UnavailableReason}).");

        return Result<ComputedOutputValueDto>.Success(new ComputedOutputValueDto
        {
            Key = output.Key,
            Value = output.Value!
        });
    }
}