using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.EnvironmentVariables.Queries.GetEnvFileForEnvironment;

public class GetEnvFileForEnvironmentQuery : IQuery<string>
{
    public Guid EnvironmentId { get; set; }
}