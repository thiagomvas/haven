using Haven.Application.Common;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Features.Manifests;
using Haven.Domain.Aggregates;

using Environment = Haven.Domain.Aggregates.Environment;

namespace Haven.Application.Features.Environments.Commands.ImportEnvironment;

public sealed class ImportEnvironmentHandler(IProjectRepository projectRepository, ManifestImporter importer)
    : Common.Messaging.ICommandHandler<ImportEnvironmentCommand, Guid>
{
    public async ValueTask<Result<Guid>> Handle(ImportEnvironmentCommand request, CancellationToken cancellationToken)
    {
        var project = await projectRepository.GetByIdAsync(request.ProjectId, cancellationToken);
        if (project is null)
            return Error.NotFoundFor(nameof(Project), request.ProjectId);

        var parsed = ManifestImporter.ParseEnvironment(request.Yaml);
        if (!parsed.IsSuccess)
            return parsed.Error;
        var manifest = parsed.Value;

        var name = string.IsNullOrWhiteSpace(request.Name) ? manifest.Name : request.Name.Trim();
        var alias = string.IsNullOrWhiteSpace(request.Alias) ? manifest.Alias : request.Alias.Trim();

        if (string.IsNullOrEmpty(alias))
            return Error.Validation("Environment alias is required: the manifest has none, so provide one.");

        if (project.Environments.Any(e => string.Equals(e.Name, name, StringComparison.OrdinalIgnoreCase)))
            return Error.ConflictFor(nameof(Environment), name);

        if (project.Environments.Any(e => string.Equals(e.Alias, alias, StringComparison.OrdinalIgnoreCase)))
            return Error.Conflicts.DuplicateAliasFor(nameof(Environment), alias);

        var environment = await importer.ImportEnvironmentAsync(
            project, manifest, name, alias, request.IncludeServices, cancellationToken);

        return Result<Guid>.CreatedFor(environment.Id);
    }
}
