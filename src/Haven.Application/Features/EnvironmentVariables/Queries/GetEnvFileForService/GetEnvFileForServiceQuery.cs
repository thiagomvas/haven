using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.EnvironmentVariables.Queries.GetEnvFileForService;

public class GetEnvFileForServiceQuery : IQuery<string>
{
    public Guid ServiceId { get; set; }
}