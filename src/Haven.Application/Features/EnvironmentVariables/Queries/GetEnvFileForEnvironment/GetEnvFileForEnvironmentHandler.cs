using Haven.Application.Common;
using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.EnvironmentVariables.Queries.GetEnvFileForEnvironment;

public class GetEnvFileForEnvironmentHandler(IEnvironmentVariableService service) : IQueryHandler<GetEnvFileForEnvironmentQuery, string>
{
    public async ValueTask<Result<string>> Handle(GetEnvFileForEnvironmentQuery query, CancellationToken cancellationToken)
    {
        var envs = await service.BuildEnvFileForEnvironmentDirectAsync(query.EnvironmentId, cancellationToken);
        return envs;
    }
}