using Haven.Domain.Aggregates;

namespace Haven.Application.Features.Services.ComputedOutputs;

public interface IComputedOutputResolver
{
    Task<IReadOnlyList<ResolvedComputedOutput>> ResolveAsync(
        Service service,
        ServiceRegistryEntry? registry,
        CancellationToken cancellationToken);
}
