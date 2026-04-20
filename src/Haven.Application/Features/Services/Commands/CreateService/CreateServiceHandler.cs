using Haven.Application.Common;
using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Domain.Aggregates;
using Environment = Haven.Domain.Entities.Environment;

namespace Haven.Application.Features.Services.Commands.CreateService;

public sealed class CreateServiceHandler(IProjectRepository projectRepository, IUnitOfWork unitOfWork)
    : Common.Messaging.ICommandHandler<CreateServiceCommand, Guid>
{
    public async ValueTask<Result<Guid>> Handle(CreateServiceCommand request, CancellationToken cancellationToken)
    {
        var project = await projectRepository.GetByIdWithServicesAsync(request.ProjectId, cancellationToken);
        if (project is null)
            return Error.NotFoundFor(nameof(Project), request.ProjectId);

        var environment = project.Environments.FirstOrDefault(e => e.Id == request.EnvironmentId);
        if (environment is null)
            return Error.NotFoundFor(nameof(Environment), request.EnvironmentId);

        if (environment.Services.Any(s => string.Equals(s.Name, request.Name, StringComparison.OrdinalIgnoreCase)))
            return Error.ConflictFor("Service", request.Name);

        var service = project.AddService(request.EnvironmentId, request.Name, request.Type, request.ExposureMode, request.ResolveSourceConfig());
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(service.Id);
    }
}
