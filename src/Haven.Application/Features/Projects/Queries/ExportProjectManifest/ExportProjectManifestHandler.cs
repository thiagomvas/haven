using Haven.Application.Common;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Messaging;
using Haven.Application.Mappers;
using Haven.Domain.Aggregates;

using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Haven.Application.Features.Projects.Queries.ExportProjectManifest;

public sealed class ExportProjectManifestHandler(IProjectRepository repository)
    : IQueryHandler<ExportProjectManifestQuery, string>
{
    private static readonly ISerializer Serializer = new SerializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull | DefaultValuesHandling.OmitEmptyCollections)
        .Build();

    public async ValueTask<Result<string>> Handle(ExportProjectManifestQuery query, CancellationToken cancellationToken)
    {
        var project = await repository.GetByIdAsync(query.ProjectId, cancellationToken);
        if (project is null)
            return Error.NotFoundFor(nameof(Project), query.ProjectId);

        var manifest = project.ToManifest(query.IncludeEnvironments, query.IncludeServices);
        return Result<string>.Success(Serializer.Serialize(manifest));
    }
}
