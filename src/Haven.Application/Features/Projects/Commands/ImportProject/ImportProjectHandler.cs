using Haven.Application.Common;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Features.Manifests;
using Haven.Domain.Aggregates;

namespace Haven.Application.Features.Projects.Commands.ImportProject;

public sealed class ImportProjectHandler(IProjectRepository projectRepository, ManifestImporter importer)
    : Common.Messaging.ICommandHandler<ImportProjectCommand, Guid>
{
    public async ValueTask<Result<Guid>> Handle(ImportProjectCommand request, CancellationToken cancellationToken)
    {
        var parsed = ManifestImporter.ParseProject(request.Yaml);
        if (!parsed.IsSuccess)
            return parsed.Error;
        var manifest = parsed.Value;

        var name = string.IsNullOrWhiteSpace(request.Name) ? manifest.Name : request.Name.Trim();
        var alias = string.IsNullOrWhiteSpace(request.Alias) ? manifest.Alias : request.Alias.Trim();

        if (await projectRepository.ExistsWithNameAsync(name, Guid.Empty, cancellationToken))
            return Error.ConflictFor(nameof(Project), name);

        var project = Project.Create(name, alias, manifest.Description);
        await projectRepository.AddAsync(project, cancellationToken);

        if (request.IncludeEnvironments)
        {
            foreach (var environment in manifest.Environments)
            {
                await importer.ImportEnvironmentAsync(
                    project, environment, environment.Name, environment.Alias, request.IncludeServices, cancellationToken);
            }
        }

        return Result<Guid>.CreatedFor(project.Id);
    }
}
