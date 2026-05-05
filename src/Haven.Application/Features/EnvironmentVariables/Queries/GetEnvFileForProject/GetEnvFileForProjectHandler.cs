using Haven.Application.Common;
using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.EnvironmentVariables.Queries.GetEnvFileForProject;

public class GetEnvFileForProjectHandler(IEnvironmentVariableService service) : IQueryHandler<GetEnvFileForProjectQuery, string>
{
    public async ValueTask<Result<string>> Handle(GetEnvFileForProjectQuery query, CancellationToken cancellationToken)
    {
        var envs = await service.BuildEnvFileForProjectAsync(query.ProjectId, cancellationToken);
        return envs;
    }
}