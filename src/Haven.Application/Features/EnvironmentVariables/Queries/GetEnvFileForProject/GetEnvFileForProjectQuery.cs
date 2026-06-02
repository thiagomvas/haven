using Haven.Application.Common;
using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.EnvironmentVariables.Queries.GetEnvFileForProject;

[RequirePermission(Permissions.Projects.View)]
public class GetEnvFileForProjectQuery : IQuery<string>
{
    public Guid ProjectId { get; set; }
}