using Haven.Application.Common;
using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.EnvironmentVariables.Queries.GetEnvFileForService;

[RequirePermission(Permissions.Services.View)]
public class GetEnvFileForServiceQuery : IQuery<string>
{
    public Guid ProjectId { get; set; }
    public Guid EnvironmentId { get; set; }
    public Guid ServiceId { get; set; }
}