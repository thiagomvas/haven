using Haven.Application.Common;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Messaging;
using Haven.Application.Mappers;

using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

using Environment = Haven.Domain.Aggregates.Environment;

namespace Haven.Application.Features.Environments.Queries.ExportEnvironmentManifest;

public sealed class ExportEnvironmentManifestHandler(IEnvironmentRepository repository)
    : IQueryHandler<ExportEnvironmentManifestQuery, string>
{
    private static readonly ISerializer Serializer = new SerializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull | DefaultValuesHandling.OmitEmptyCollections)
        .Build();

    public async ValueTask<Result<string>> Handle(ExportEnvironmentManifestQuery query, CancellationToken cancellationToken)
    {
        var environment = await repository.GetByIdWithServicesAsync(query.EnvironmentId, cancellationToken);
        if (environment is null)
            return Error.NotFoundFor(nameof(Environment), query.EnvironmentId);

        var manifest = environment.ToManifest(query.IncludeServices);
        return Result<string>.Success(Serializer.Serialize(manifest));
    }
}
