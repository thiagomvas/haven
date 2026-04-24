using Haven.Application.Common;
using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Domain.Aggregates;


namespace Haven.Application.Features.Projects.Commands.CreateProject;

public sealed class CreateProjectHandler(IProjectRepository projectRepository, IUnitOfWork unitOfWork)
    : Common.Messaging.ICommandHandler<CreateProjectCommand, Guid>
{
    public async ValueTask<Result<Guid>> Handle(CreateProjectCommand request, CancellationToken cancellationToken)
    {
        var exists = await projectRepository.ExistsWithNameAsync(request.Name, Guid.Empty, cancellationToken);
        if (exists)
            return Error.ConflictFor(nameof(Project), request.Name);
        
        var project = Project.Create(request.Name, request.Description);
        var projectId = await projectRepository.AddAsync(project, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        
        return Result<Guid>.CreatedFor(projectId);
    }
}