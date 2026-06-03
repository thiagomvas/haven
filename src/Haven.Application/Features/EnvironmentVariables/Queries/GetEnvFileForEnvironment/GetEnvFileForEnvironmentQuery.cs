using Haven.Application.Common;
using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.EnvironmentVariables.Queries.GetEnvFileForEnvironment;

[RequirePermission(Permissions.ProjectManagement.Read)]
public class GetEnvFileForEnvironmentQuery : IQuery<string>
{
    public Guid ProjectId { get; set; }
    public Guid EnvironmentId { get; set; }
}