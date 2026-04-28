using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.Environments.Queries.GetEnvironment;

public sealed class GetEnvironmentQuery : IQuery<EnvironmentDto>
{
    public Guid ProjectId { get; init; }
    public Guid EnvironmentId { get; init; }
}
