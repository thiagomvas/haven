using Haven.Application.Common;
using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Domain.Aggregates;

using Environment = Haven.Domain.Entities.Environment;


namespace Haven.Application.Features.Services.Commands.CreateService;

public sealed class CreateServiceHandler(IProjectRepository projectRepository, IServiceRepository serviceRepository)
    : Common.Messaging.ICommandHandler<CreateServiceCommand, Guid>
{
    public async ValueTask<Result<Guid>> Handle(CreateServiceCommand request, CancellationToken cancellationToken)
    {
        var project = await projectRepository.GetByIdAsync(request.ProjectId, cancellationToken);
        if (project is null)
            return Error.NotFoundFor(nameof(Project), request.ProjectId);

        var environment = project.Environments.FirstOrDefault(e => e.Id == request.EnvironmentId);
        if (environment is null)
            return Error.NotFoundFor(nameof(Environment), request.EnvironmentId);

        if (environment.Services.Any(s => string.Equals(s.Name, request.Name, StringComparison.OrdinalIgnoreCase)))
            return Error.ConflictFor("Service", request.Name);
        
        if (environment.Services.Any(s => string.Equals(s.Alias, request.Alias, StringComparison.OrdinalIgnoreCase)))
            return Error.ConflictFor("Service alias", request.Alias);

        var service = project.AddService(request.EnvironmentId, request.Name, request.Type, request.ExposureMode, request.Alias, request.ResolveSourceConfig());
        await serviceRepository.AddAsync(service, cancellationToken);
        return Result<Guid>.CreatedFor(service.Id);
    }
}