using Haven.Application.Common;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Interfaces;
using Haven.Domain.Aggregates;

namespace Haven.Application.Projects.Commands.CreateProject;

public sealed class CreateProjectHandler(IProjectRepository projectRepository, IUnitOfWork unitOfWork)
    : Common.Messaging.ICommandHandler<CreateProjectCommand, Guid>
{
    public async ValueTask<Result<Guid>> Handle(CreateProjectCommand request, CancellationToken cancellationToken)
    {
        var project = Project.Create(request.Name, request.Description);
        var projectId = await projectRepository.AddAsync(project, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(projectId);
    }
}