using Haven.Application.Common;
using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Domain.Aggregates;

namespace Haven.Application.Features.Environments.Commands.CreateEnvironment;

public sealed class CreateEnvironmentHandler(IProjectRepository projectRepository, IUnitOfWork unitOfWork)
    : Common.Messaging.ICommandHandler<CreateEnvironmentCommand, Guid>
{
    public async ValueTask<Result<Guid>> Handle(CreateEnvironmentCommand request, CancellationToken cancellationToken)
    {
        var project = await projectRepository.GetByIdWithEnvironmentsAsync(request.ProjectId, cancellationToken);
        if (project is null)
            return Error.NotFoundFor(nameof(Project), request.ProjectId);

        if (project.Environments.Any(e => string.Equals(e.Name, request.Name, StringComparison.OrdinalIgnoreCase)))
            return Error.ConflictFor("Environment", request.Name);

        var environment = project.AddEnvironment(request.Name, request.Description);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(environment.Id);
    }
}
