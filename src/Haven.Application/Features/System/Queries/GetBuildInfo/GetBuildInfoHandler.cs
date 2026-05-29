using Haven.Application.Common;
using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.System.Queries.GetBuildInfo;

public sealed class GetBuildInfoHandler(IBuildInfoService buildInfoService)
    : IQueryHandler<GetBuildInfoQuery, BuildInfoDto>
{
    public async ValueTask<Result<BuildInfoDto>> Handle(GetBuildInfoQuery query, CancellationToken cancellationToken)
    {
        var info = await buildInfoService.GetAsync(cancellationToken);

        return Result<BuildInfoDto>.Success(new BuildInfoDto
        {
            Version = info.Version,
            CommitSha = info.CommitSha,
            BuildDate = info.BuildDate,
            BuildSystem = info.BuildSystem,
            DotNetVersion = info.DotNetVersion,
            Database = new DatabaseBuildInfoDto
            {
                Provider = info.Database.Provider,
                Version = info.Database.Version,
                Path = info.Database.Path,
            },
            DockerEngine = new DockerEngineBuildInfoDto
            {
                IsConnected = info.DockerEngine.IsConnected,
                Version = info.DockerEngine.Version,
            }
        });
    }
}
