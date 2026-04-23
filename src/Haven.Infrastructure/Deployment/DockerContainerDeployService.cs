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
using ServiceStatus = Haven.Domain.ServiceStatus;

namespace Haven.Infrastructure.Deployment;

public class DockerContainerDeployService : IDeployService
{
    private readonly ILogger<DockerContainerDeployService> _logger;
    private readonly HavenDbContext _db;
    private readonly IDockerClient _dockerClient;

    public DockerContainerDeployService(ILogger<DockerContainerDeployService> logger, HavenDbContext db,
        IDockerClient dockerClient)
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

        await RemoveExistingContainerAsync(service, cancellationToken);

        _logger.LogInformation(
            "Pulling Docker image '{Image}' for service '{ServiceName}' from project '{ProjectName}'",
            dockerConfig.Image,
            service.Name,
            project.Name);

        try
        {
            await _dockerClient.Images.DeleteImageAsync(dockerConfig.Image, new ImageDeleteParameters { Force = true },
                cancellationToken);
        }
        catch
        {
            _logger.LogDebug("Could not remove old image '{Image}', proceeding with pull", dockerConfig.Image);
        }

        await _dockerClient.Images.CreateImageAsync(new ImagesCreateParameters { FromImage = dockerConfig.Image },
            null,
            new Progress<JSONMessage>(),
            cancellationToken);

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

        if (service.ExposureMode is ExposureMode.Internal or ExposureMode.External)
        {
            var listenAddress = service.ExposureMode == ExposureMode.Internal ? "127.0.0.1" : "0.0.0.0";
            var envVars = new List<string>(dockerConfig.EnvironmentVariables) { $"LISTEN_ADDRESS={listenAddress}" };
            param.Env = envVars;

            if (dockerConfig.Ports.Count > 0)
            {
                param.ExposedPorts = new Dictionary<string, EmptyStruct>();
                var portBindings = new Dictionary<string, IList<PortBinding>>();

                foreach (var portMapping in dockerConfig.Ports)
                {
                    var parts = portMapping.Split(':');
                    var hostPort = parts[0];
                    var containerPort = parts.Length > 1 ? parts[1] : hostPort;

                    var portKey = containerPort.Contains("/") ? containerPort : $"{containerPort}/tcp";
                    param.ExposedPorts[portKey] = default;

                    portBindings[portKey] = new List<PortBinding>
                    {
                        new PortBinding { HostIP = listenAddress, HostPort = hostPort }
                    };
                }

                param.HostConfig = new HostConfig { PortBindings = portBindings };
            }
        }
        else if (dockerConfig.EnvironmentVariables.Count > 0)
        {
            param.Env = new List<string>(dockerConfig.EnvironmentVariables);
        }

        var response = await _dockerClient.Containers.CreateContainerAsync(param, cancellationToken);

        var started =
            await _dockerClient.Containers.StartContainerAsync(response.ID, new ContainerStartParameters(),
                cancellationToken);

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

    public async Task<Result> StopAsync(Service service, CancellationToken cancellationToken)
    {
        var idLabel = DockerUtils.BuildIdLabel(service.Id);
        var param = new ContainersListParameters()
        {
            All = true,
            Filters = new Dictionary<string, IDictionary<string, bool>>
            {
                {
                    "label",
                    new Dictionary<string, bool>
                    {
                        { $"{idLabel.Key}={idLabel.Value}", true }
                    }
                }
            }
        };

        var containers = await _dockerClient.Containers.ListContainersAsync(param, cancellationToken);

        if (containers.Count == 0)
        {
            _logger.LogWarning("No Docker container found for service '{ServiceName}' to stop", service.Name);

            if (service.Status == ServiceStatus.Running)
            {
                service.Environment?.Project?.StopService(service.EnvironmentId, service.Id);
                await _db.SaveChangesAsync(cancellationToken);
            }

            return Error.NotFoundFor("Docker Container", service.Id);
        }

        foreach (var container in containers)
        {
            await _dockerClient.Containers.StopContainerAsync(container.ID, new ContainerStopParameters(), cancellationToken);
            await _dockerClient.Containers.RemoveContainerAsync(container.ID, new ContainerRemoveParameters { Force = true }, cancellationToken);
            _logger.LogInformation("Stopped and removed Docker container '{ContainerId}' for service '{ServiceName}'", container.ID, service.Name);
        }

        return Result.Success();
    }

    public async Task<Result> RestartAsync(Service service, CancellationToken cancellationToken)
    {
        var environment = service.Environment;
        if (environment == null) return Error.NotFoundFor(nameof(Environment), service.EnvironmentId);
        var project = environment.Project;
        if (project == null) return Error.NotFoundFor(nameof(Project), environment.ProjectId);

        var dockerConfig = service.SourceConfig as DockerConfig;
        if (dockerConfig == null || string.IsNullOrWhiteSpace(dockerConfig.Image))
            return Error.Validation;

        await RemoveExistingContainerAsync(service, cancellationToken);

        _logger.LogInformation(
            "Restarting service '{ServiceName}' from project '{ProjectName}'",
            service.Name,
            project.Name);

        var param = new CreateContainerParameters()
        {
            Name = DockerUtils.BuildContainerName(service.Name, service.Id),
            Labels = DockerUtils.BuildContainerLabels(service),
            Image = dockerConfig.Image,
        };

        if (service.ExposureMode is ExposureMode.Internal or ExposureMode.External)
        {
            var listenAddress = service.ExposureMode == ExposureMode.Internal ? "127.0.0.1" : "0.0.0.0";
            var envVars = new List<string>(dockerConfig.EnvironmentVariables) { $"LISTEN_ADDRESS={listenAddress}" };
            param.Env = envVars;

            if (dockerConfig.Ports.Count > 0)
            {
                param.ExposedPorts = new Dictionary<string, EmptyStruct>();
                var portBindings = new Dictionary<string, IList<PortBinding>>();

                foreach (var portMapping in dockerConfig.Ports)
                {
                    var parts = portMapping.Split(':');
                    var hostPort = parts[0];
                    var containerPort = parts.Length > 1 ? parts[1] : hostPort;

                    var portKey = containerPort.Contains("/") ? containerPort : $"{containerPort}/tcp";
                    param.ExposedPorts[portKey] = default;

                    portBindings[portKey] = new List<PortBinding>
                    {
                        new PortBinding { HostIP = listenAddress, HostPort = hostPort }
                    };
                }

                param.HostConfig = new HostConfig { PortBindings = portBindings };
            }
        }
        else if (dockerConfig.EnvironmentVariables.Count > 0)
        {
            param.Env = new List<string>(dockerConfig.EnvironmentVariables);
        }

        var response = await _dockerClient.Containers.CreateContainerAsync(param, cancellationToken);

        var started = await _dockerClient.Containers.StartContainerAsync(response.ID, new ContainerStartParameters(),
            cancellationToken);

        if (!started)
        {
            _logger.LogError("Failed to start Docker container for service '{ServiceName}'", service.Name);
            return Error.Validation;
        }

        _logger.LogInformation(
            "Successfully restarted service '{ServiceName}' from project '{ProjectName}'",
            service.Name,
            project.Name);

        return Result.Success();
    }

    private async Task RemoveExistingContainerAsync(Service service, CancellationToken cancellationToken)
    {
        var idLabel = DockerUtils.BuildIdLabel(service.Id);
        var param = new ContainersListParameters()
        {
            All = true,
            Filters = new Dictionary<string, IDictionary<string, bool>>
            {
                {
                    "label",
                    new Dictionary<string, bool>
                    {
                        { $"{idLabel.Key}={idLabel.Value}", true }
                    }
                }
            }
        };

        var containers = await _dockerClient.Containers.ListContainersAsync(param, cancellationToken);

        foreach (var container in containers)
        {
            if (container.State == "running")
            {
                await _dockerClient.Containers.StopContainerAsync(container.ID, new ContainerStopParameters(), cancellationToken);
            }

            await _dockerClient.Containers.RemoveContainerAsync(container.ID, new ContainerRemoveParameters { Force = true }, cancellationToken);
            _logger.LogInformation("Removed existing Docker container '{ContainerId}' for service '{ServiceName}' before deploying new version", container.ID, service.Name);
        }
    }
}