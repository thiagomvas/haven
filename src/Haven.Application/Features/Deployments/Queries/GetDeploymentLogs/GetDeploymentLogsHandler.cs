using Haven.Application.Common;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.Deployments.Queries.GetDeploymentLogs;

public sealed class GetDeploymentLogsHandler(IDeploymentRepository deploymentRepository)
    : IQueryHandler<GetDeploymentLogsQuery, string[]>
{
    public async ValueTask<Result<string[]>> Handle(GetDeploymentLogsQuery query, CancellationToken cancellationToken)
    {
        var deployment = await deploymentRepository.FindByIdAsync(query.DeploymentId, cancellationToken);
        if (deployment is null)
            return Error.NotFoundFor("Deployment", query.DeploymentId);

        if (!File.Exists(deployment.LogFile))
            return Result<string[]>.Success([]);

        var lines = await ReadLogLinesAsync(deployment.LogFile, cancellationToken);
        return Result<string[]>.Success(lines);
    }

    private static async Task<string[]> ReadLogLinesAsync(string path, CancellationToken ct)
    {
        // FileShare.Read because the writer holds FileShare.Read on its end
        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var reader = new StreamReader(stream);
        var content = await reader.ReadToEndAsync(ct);
        return content.Split('\n', StringSplitOptions.RemoveEmptyEntries);
    }
}
