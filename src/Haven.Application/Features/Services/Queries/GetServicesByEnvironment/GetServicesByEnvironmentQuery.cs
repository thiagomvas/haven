using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.Services.Queries.GetServicesByEnvironment;

public sealed class GetServicesByEnvironmentQuery : IQuery<IReadOnlyList<ServiceDto>>
{
    public Guid ProjectId { get; init; }
    public Guid EnvironmentId { get; init; }
}
