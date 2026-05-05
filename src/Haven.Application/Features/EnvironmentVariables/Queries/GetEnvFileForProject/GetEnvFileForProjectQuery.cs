using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.EnvironmentVariables.Queries.GetEnvFileForProject;

public class GetEnvFileForProjectQuery : IQuery<string>
{
    public Guid ProjectId { get; set; }
}