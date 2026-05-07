using Haven.Application.Common;
using Haven.Application.Common.Messaging;
using Haven.Application.Configuration;
using Haven.Application.Features.Configuration.Dtos;
using Microsoft.Extensions.Options;

namespace Haven.Application.Features.Configuration.Queries.GetConfiguration;

public sealed class GetConfigurationHandler(IOptionsMonitor<ManifestsOptions> manifests)
    : IQueryHandler<GetConfigurationQuery, HavenConfigurationDto>
{
    public async ValueTask<Result<HavenConfigurationDto>> Handle(GetConfigurationQuery request, CancellationToken ct)
    {
        var dto = new HavenConfigurationDto(manifests.CurrentValue);
        return await ValueTask.FromResult(Result<HavenConfigurationDto>.Success(dto));
    }
}
