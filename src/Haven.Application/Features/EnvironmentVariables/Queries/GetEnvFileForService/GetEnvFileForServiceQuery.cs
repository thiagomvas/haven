using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.EnvironmentVariables.Queries.GetEnvFileForService;

public class GetEnvFileForServiceQuery : IQuery<string>
{
    public Guid ProjectId { get; set; }
    public Guid EnvironmentId { get; set; }
    public Guid ServiceId { get; set; }
}