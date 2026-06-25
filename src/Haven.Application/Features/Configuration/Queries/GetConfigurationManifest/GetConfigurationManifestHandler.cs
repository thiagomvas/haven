using Haven.Application.Common;
using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.Configuration.Queries.GetConfigurationManifest;

public sealed class GetConfigurationManifestHandler(IHavenConfigurationSerializer serializer)
    : IQueryHandler<GetConfigurationManifestQuery, string>
{
    public async ValueTask<Result<string>> Handle(GetConfigurationManifestQuery query, CancellationToken cancellationToken)
    {
        var yaml = await serializer.ReadRawAsync(cancellationToken);
        return Result<string>.Success(yaml);
    }
}