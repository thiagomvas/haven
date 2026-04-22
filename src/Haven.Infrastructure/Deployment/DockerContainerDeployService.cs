using Haven.Application.Common;
using Haven.Application.Common.Interfaces.Deployment;
using Haven.Domain;
using Haven.Domain.Aggregates;
using Haven.Domain.Entities;
using Haven.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Environment = Haven.Domain.Entities.Environment;

namespace Haven.Infrastructure.Deployment;

public class DockerContainerDeployService : IDeployService
{
    private readonly ILogger<DockerContainerDeployService> _logger;
    private readonly HavenDbContext _db;

    public DockerContainerDeployService(ILogger<DockerContainerDeployService> logger, HavenDbContext db)
    {
        _logger = logger;
        _db = db;
    }

    public ServiceType ServiceType => ServiceType.DockerImage;

    public async Task<Result> DeployAsync(Service service, CancellationToken cancellationToken)
    {
        var environment = service.Environment;
        if (environment == null) return Error.NotFoundFor(nameof(Environment), service.EnvironmentId);
        var project = environment.Project;
        if (project == null) return Error.NotFoundFor(nameof(Project), environment.ProjectId);

        _logger.LogInformation(
            "Deploying service '{ServiceName}' from project '{ProjectName}' as a Docker Container",
            service.Name,
            project.Name);

        project.DeployService(service.EnvironmentId, service.Id);
        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}