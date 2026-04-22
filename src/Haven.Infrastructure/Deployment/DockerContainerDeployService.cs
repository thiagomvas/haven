using Docker.DotNet;
using Docker.DotNet.Models;
using Haven.Application.Common;
using Haven.Application.Common.Interfaces.Deployment;
using Haven.Domain;
using Haven.Domain.Aggregates;
using Haven.Domain.Entities;
using Haven.Domain.ValueObjects;
using Haven.Infrastructure.Persistence;
using Haven.Infrastructure.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Environment = Haven.Domain.Entities.Environment;

namespace Haven.Infrastructure.Deployment;

public class DockerContainerDeployService : IDeployService
{
    private readonly ILogger<DockerContainerDeployService> _logger;
    private readonly HavenDbContext _db;
    private readonly IDockerClient _dockerClient;

    public DockerContainerDeployService(ILogger<DockerContainerDeployService> logger, HavenDbContext db, IDockerClient dockerClient)
    {
        _logger = logger;
        _db = db;
        _dockerClient = dockerClient;
    }

    public ServiceType ServiceType => ServiceType.DockerImage;

    public async Task<Result> DeployAsync(Service service, CancellationToken cancellationToken)
    {
        var environment = service.Environment;
        if (environment == null) return Error.NotFoundFor(nameof(Environment), service.EnvironmentId);
        var project = environment.Project;
        if (project == null) return Error.NotFoundFor(nameof(Project), environment.ProjectId);

        var dockerConfig = service.SourceConfig as DockerConfig;
        if (dockerConfig == null || string.IsNullOrWhiteSpace(dockerConfig.Image))
            return Error.Validation;

        _logger.LogInformation(
            "Pulling Docker image '{Image}' for service '{ServiceName}' from project '{ProjectName}'",
            dockerConfig.Image,
            service.Name,
            project.Name);

        await _dockerClient.Images.CreateImageAsync(new ImagesCreateParameters { FromImage = dockerConfig.Image },
            null,
            new Progress<JSONMessage>()
            , cancellationToken);

        _logger.LogInformation(
            "Deploying service '{ServiceName}' from project '{ProjectName}' as a Docker Container",
            service.Name,
            project.Name);
        
        var param = new CreateContainerParameters()
        {
            Name = DockerUtils.BuildContainerName(service.Name, service.Id),
            Labels = DockerUtils.BuildContainerLabels(service),
            Image = dockerConfig.Image,
        };
        
        var response = await _dockerClient.Containers.CreateContainerAsync(param, cancellationToken);
        
        var started = await _dockerClient.Containers.StartContainerAsync(response.ID, new ContainerStartParameters(), cancellationToken);

        if (!started)
        {
            _logger.LogError("Failed to start Docker container for service '{ServiceName}'", service.Name);
            return Error.Validation;
        }

        project.DeployService(service.EnvironmentId, service.Id);
        await _db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation(
            "Successfully deployed service '{ServiceName}' from project '{ProjectName}' as a Docker Container",
            service.Name,
            project.Name);
        return Result.Success();
    }
}