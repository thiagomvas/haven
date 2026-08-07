using Haven.Application.Common;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Messaging;
using Haven.Domain.Aggregates;

using Environment = Haven.Domain.Aggregates.Environment;

namespace Haven.Application.Features.Services.Commands.UpdateService;

public sealed class UpdateServiceHandler(
    IProjectRepository projectRepository) : ICommandHandler<UpdateServiceCommand, Guid>
{
    public async ValueTask<Result<Guid>> Handle(UpdateServiceCommand request, CancellationToken cancellationToken)
    {
        var project = await projectRepository.GetByIdAsync(request.ProjectId, cancellationToken);
        if (project is null)
            return Error.NotFoundFor(nameof(Project), request.ProjectId);

        var environment = project.Environments.FirstOrDefault(e => e.Id == request.EnvironmentId);
        if (environment is null)
            return Error.NotFoundFor(nameof(Environment), request.EnvironmentId);

        var service = environment.Services.FirstOrDefault(s => s.Id == request.ServiceId);
        if (service is null)
            return Error.NotFoundFor("Service", request.ServiceId);

        if (request.Name.HasValue && !string.Equals(request.Name.Value, service.Name, StringComparison.OrdinalIgnoreCase))
        {
            var nameConflict = environment.Services.Any(s =>
                s.Id != request.ServiceId &&
                string.Equals(s.Name, request.Name.Value, StringComparison.OrdinalIgnoreCase));
            if (nameConflict)
                return Error.ConflictFor("Service", request.Name.Value);
        }

        var sourceConfig = request.ResolveSourceConfig();
        environment.UpdateService(request.ServiceId, request.Name, request.Type, request.ExposureMode, request.Alias, sourceConfig);

        return Result<Guid>.Success(request.ServiceId);
    }
}