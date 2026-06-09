using Haven.Application.Common;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Domain.Aggregates;

using Environment = Haven.Domain.Entities.Environment;

namespace Haven.Application.Features.Services.Commands.RegenerateServiceToken;

public sealed class RegenerateServiceTokenHandler(IProjectRepository projectRepository, IServiceRepository serviceRepository)
    : Common.Messaging.ICommandHandler<RegenerateServiceTokenCommand, string>
{
    public async ValueTask<Result<string>> Handle(RegenerateServiceTokenCommand request, CancellationToken cancellationToken)
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

        service.RegenerateToken();

        return Result<string>.Success(service.Token);
    }
}