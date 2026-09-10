using Haven.Application.Common;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Interfaces.Shell;
using Haven.Domain.Aggregates;

using Environment = Haven.Domain.Aggregates.Environment;

namespace Haven.Application.Features.Services.Commands.OpenShellSession;

public sealed class OpenShellSessionHandler(
    IProjectRepository projectRepository,
    IContainerShellService shellService)
    : Common.Messaging.ICommandHandler<OpenShellSessionCommand, IShellSession>
{
    public async ValueTask<Result<IShellSession>> Handle(OpenShellSessionCommand request, CancellationToken cancellationToken)
    {
        var project = await projectRepository.GetByIdAsync(request.ProjectId, cancellationToken);
        if (project is null)
            return Error.NotFoundFor(nameof(Project), request.ProjectId);

        var environment = project.Environments.FirstOrDefault(e => e.Id == request.EnvironmentId);
        if (environment is null)
            return Error.NotFoundFor(nameof(Environment), request.EnvironmentId);

        var service = environment.Services.FirstOrDefault(s => s.Id == request.ServiceId);
        if (service is null)
            return Error.NotFoundFor(nameof(Service), request.ServiceId);

        return await shellService.CreateSessionAsync(service.Id, request.ShellType, cancellationToken);
    }
}
