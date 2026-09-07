using Haven.Application.Common;
using Haven.Application.Common.Interfaces.Deployment;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Messaging;
using Haven.Application.Features.Services.Shared;
using Haven.Domain.Aggregates;
using Haven.Domain.Exceptions;

using Environment = Haven.Domain.Aggregates.Environment;

namespace Haven.Application.Features.Services.Commands.BulkRestartServices;

public sealed class BulkRestartServicesHandler(
    IProjectRepository projectRepository,
    IDeploymentJobEnqueuer deploymentJobEnqueuer)
    : ICommandHandler<BulkRestartServicesCommand, BulkServiceActionResponse>
{
    public async ValueTask<Result<BulkServiceActionResponse>> Handle(
        BulkRestartServicesCommand request,
        CancellationToken cancellationToken)
    {
        var project = await projectRepository.GetByIdAsync(request.ProjectId, cancellationToken);
        if (project is null)
            return Error.NotFoundFor(nameof(Project), request.ProjectId);

        var environment = project.Environments.FirstOrDefault(e => e.Id == request.EnvironmentId);
        if (environment is null)
            return Error.NotFoundFor(nameof(Environment), request.EnvironmentId);

        var results = new List<BulkServiceActionResult>();
        foreach (var serviceId in request.ServiceIds)
        {
            var service = environment.Services.FirstOrDefault(s => s.Id == serviceId);
            if (service is null)
            {
                results.Add(new BulkServiceActionResult(serviceId, false, $"Service '{serviceId}' was not found."));
                continue;
            }

            try
            {
                deploymentJobEnqueuer.EnqueueRestart(request.ProjectId, request.EnvironmentId, serviceId);
                results.Add(new BulkServiceActionResult(serviceId, true, null));
            }
            catch (HavenException ex)
            {
                results.Add(new BulkServiceActionResult(serviceId, false, ex.Message));
            }
        }

        return Result<BulkServiceActionResponse>.Success(new BulkServiceActionResponse(results));
    }
}
