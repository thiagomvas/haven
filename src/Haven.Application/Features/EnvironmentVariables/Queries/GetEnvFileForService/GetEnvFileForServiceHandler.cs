using Haven.Application.Common;
using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.EnvironmentVariables.Queries.GetEnvFileForService;

public class GetEnvFileForServiceHandler(IEnvironmentVariableService service) : IQueryHandler<GetEnvFileForServiceQuery, string>
{
    public async ValueTask<Result<string>> Handle(GetEnvFileForServiceQuery query, CancellationToken cancellationToken)
    {
        var envs = await service.BuildEnvFileForServiceDirectAsync(query.ServiceId, cancellationToken);
        return envs;
    }
}