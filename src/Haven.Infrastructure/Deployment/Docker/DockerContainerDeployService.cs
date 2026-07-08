using System.Net;

using Docker.DotNet;
using Docker.DotNet.Models;

using Haven.Application.Common;
using Haven.Application.Common.Contracts;
using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Interfaces.Deployment;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Configuration;
using Haven.Domain;
using Haven.Domain.Aggregates;
using Haven.Domain.Entities;
using Haven.Domain.ValueObjects;
using Haven.Infrastructure.Persistence;
using Haven.Infrastructure.Utils;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Environment = Haven.Domain.Entities.Environment;
using ServiceStatus = Haven.Domain.ServiceStatus;

namespace Haven.Infrastructure.Deployment;

public class DockerContainerDeployService : IDeployService
{
    private readonly ILogger<DockerContainerDeployService> _logger;
    private readonly HavenDbContext _db;
    private readonly IDockerClient _dockerClient;
    private readonly INetworkingService _networkingService;
    private readonly IEnvironmentVariableService _environmentVariableService;
    private readonly IFeatureFlagService _featureFlagService;
    private readonly IDeploymentLogService _logService;
    private readonly IOptionsMonitor<VolumesOptions> _volumesOptions;

    public DockerContainerDeployService(ILogger<DockerContainerDeployService> logger, HavenDbContext db,
        IDockerClient dockerClient,
        INetworkingServiceFactory networkingServiceFactory, IEnvironmentVariableService environmentVariableService,
        IFeatureFlagService featureFlagService, IDeploymentLogService logService,
        IOptionsMonitor<VolumesOptions> volumesOptions)
    {
        _logger = logger;
        _db = db;
        _dockerClient = dockerClient;
        _environmentVariableService = environmentVariableService;
        _featureFlagService = featureFlagService;
        _logService = logService;
        _volumesOptions = volumesOptions;
        _networkingService = networkingServiceFactory.Create(ServiceType.DockerImage) ?? throw new InvalidOperationException("No networking service found for DockerImage type");
    }

    public ServiceType ServiceType => ServiceType.DockerImage;

    public async Task<Result<DeployData>> DeployAsync(Service service, Guid deploymentId, CancellationToken cancellationToken)
    {
        var environment = service.Environment;
        if (environment == null) return Error.NotFoundFor(nameof(Environment), service.EnvironmentId);
        var project = environment.Project;
        if (project == null) return Error.NotFoundFor(nameof(Project), environment.ProjectId);

        var dockerConfig = service.SourceConfig as DockerConfig;
        if (dockerConfig == null || string.IsNullOrWhiteSpace(dockerConfig.Image))
            return Error.InvalidSourceConfig;

        await _networkingService.DisconnectServiceFromAllNetworksAsync(service.Id, cancellationToken);
        await RemoveExistingContainerAsync(service, cancellationToken);

        _logger.LogInformation(
            "Pulling Docker image '{Image}' for service '{ServiceName}' from project '{ProjectName}'",
            dockerConfig.Image,
            service.Name,
            project.Name);

        await _logService.AppendLogAsync(deploymentId, $"Pulling image '{dockerConfig.Image}'...", cancellationToken);

        try
        {
            await _dockerClient.Images.DeleteImageAsync(dockerConfig.Image, new ImageDeleteParameters { Force = true },
                cancellationToken);
        }
        catch
        {
            _logger.LogDebug("Could not remove old image '{Image}', proceeding with pull", dockerConfig.Image);
        }

        var pullProgress = new Progress<JSONMessage>(msg =>
        {
            if (!string.IsNullOrWhiteSpace(msg.Status))
                _ = _logService.AppendLogAsync(deploymentId, msg.Status, cancellationToken);
        });

        try
        {
            await _dockerClient.Images.CreateImageAsync(new ImagesCreateParameters { FromImage = dockerConfig.Image },
                null,
                pullProgress,
                cancellationToken);
        }
        catch (DockerApiException ex)
        {
            _logger.LogError(ex, "Failed to pull Docker image '{Image}' for service '{ServiceName}'", dockerConfig.Image, service.Name);
            await _logService.AppendLogAsync(deploymentId, $"Failed to pull image '{dockerConfig.Image}': {ex.Message}", cancellationToken);
            return Error.Docker.InvalidImage;
        }

        await _logService.AppendLogAsync(deploymentId, $"Image '{dockerConfig.Image}' pulled successfully.", cancellationToken);

        _logger.LogInformation(
            "Deploying service '{ServiceName}' from project '{ProjectName}' as a Docker Container",
            service.Name,
            project.Name);

        var envs = await _environmentVariableService.BuildVariablesForServiceAsync(service.Id, cancellationToken);
        var flags = await _featureFlagService.GetFlagsAsEnvironmentsForServiceAsync(service.Id, cancellationToken);
        envs.AddRange(flags);

        var param = BuildCreateContainerParameters(service, dockerConfig, envs.ToList());

        await _logService.AppendLogAsync(deploymentId, "Creating and starting container...", cancellationToken);
        var createResult = await CreateAndStartContainerAsync(param, service, cancellationToken);

        if (createResult.IsFailure)
        {
            await _logService.AppendLogAsync(deploymentId, "Failed to start container.", cancellationToken);
            return createResult.Error;
        }

        await _logService.AppendLogAsync(deploymentId, "Container started successfully.", cancellationToken);

        _logger.LogInformation(
            "Successfully deployed service '{ServiceName}' from project '{ProjectName}' as a Docker Container",
            service.Name,
            project.Name);

        var inspect = await _dockerClient.Containers.InspectContainerAsync(createResult.Value, cancellationToken);
        var rawIp = inspect.NetworkSettings.Networks.Values
            .Select(n => n.IPAddress)
            .FirstOrDefault(ip => !string.IsNullOrEmpty(ip));

        return new DeployData
        {
            ServiceId = service.Id,
            IpAddress = rawIp != null ? IPAddress.Parse(rawIp) : null,
            ContainerName = param.Name,
            Ports = inspect.ExtractPortMappings()
        };
    }

    public async Task<Result> StopAsync(Service service, CancellationToken cancellationToken)
    {
        var containers = await GetContainersForServiceAsync(service, cancellationToken);

        if (containers.Count == 0)
        {
            _logger.LogWarning("No Docker container found for service '{ServiceName}' to stop", service.Name);
            return Error.NotFoundFor("Docker Container", service.Id);
        }

        await StopAndRemoveContainersAsync(containers, service, "Stopped and removed Docker container '{ContainerId}' for service '{ServiceName}'", cancellationToken);

        return Result.Success();
    }

    public async Task<Result<DeployData>> StartAsync(Service service, CancellationToken cancellationToken)
    {
        var environment = service.Environment;
        if (environment == null) return Error.NotFoundFor(nameof(Environment), service.EnvironmentId);
        var project = environment.Project;
        if (project == null) return Error.NotFoundFor(nameof(Project), environment.ProjectId);

        var dockerConfig = service.SourceConfig as DockerConfig;
        if (dockerConfig == null || string.IsNullOrWhiteSpace(dockerConfig.Image))
            return Error.InvalidSourceConfig;

        _logger.LogInformation(
            "Starting service '{ServiceName}' from project '{ProjectName}'",
            service.Name,
            project.Name);

        var envs = await _environmentVariableService.BuildVariablesForServiceAsync(service.Id, cancellationToken);
        var flags = await _featureFlagService.GetFlagsAsEnvironmentsForServiceAsync(service.Id, cancellationToken);
        envs.AddRange(flags);

        var param = BuildCreateContainerParameters(service, dockerConfig, envs.ToList());
        var createResult = await CreateAndStartContainerAsync(param, service, cancellationToken);

        if (createResult.IsFailure)
            return createResult.Error;

        _logger.LogInformation(
            "Successfully started service '{ServiceName}' from project '{ProjectName}'",
            service.Name,
            project.Name);

        var inspect = await _dockerClient.Containers.InspectContainerAsync(createResult.Value, cancellationToken);
        var rawIp = inspect.NetworkSettings.Networks.Values
            .Select(n => n.IPAddress)
            .FirstOrDefault(ip => !string.IsNullOrEmpty(ip));

        return new DeployData
        {
            ServiceId = service.Id,
            IpAddress = rawIp != null ? IPAddress.Parse(rawIp) : null,
            ContainerName = param.Name,
            Ports = inspect.ExtractPortMappings()
        };
    }

    public async Task CleanupAsync(Service service, CancellationToken cancellationToken)
    {
        var containers = await GetContainersForServiceAsync(service, cancellationToken);
        if (containers.Count > 0)
            await StopAndRemoveContainersAsync(containers, service, "Cleaned up Docker container '{ContainerId}' for deleted service '{ServiceName}'", cancellationToken);
    }

    private CreateContainerParameters BuildCreateContainerParameters(Service service, DockerConfig dockerConfig, List<EnvironmentVariables>? envs = null)
    {
        var param = new CreateContainerParameters()
        {
            Name = DockerUtils.BuildContainerName(service.Environment?.Project?.Alias, service.Environment?.Alias, service.Alias, service.Name, service.Id),
            Labels = DockerUtils.BuildContainerLabels(service),
            Image = dockerConfig.Image,
        };

        var envVars = (envs ?? []).Select(e => $"{e.Key}={e.Value}").ToList();
        var hostConfig = new HostConfig();

        _logger.LogDebug("Building container parameters for service '{ServiceName}': ExposureMode={ExposureMode}, PortCount={PortCount}",
            service.Name, service.ExposureMode, dockerConfig.Ports.Count);

        if (service.ExposureMode is ExposureMode.Internal or ExposureMode.External)
        {
            var listenAddress = service.ExposureMode == ExposureMode.Internal ? "127.0.0.1" : "0.0.0.0";
            envVars.Add($"LISTEN_ADDRESS={listenAddress}");

            if (dockerConfig.Ports.Count > 0)
            {
                var exposedPorts = new Dictionary<string, EmptyStruct>();
                var portBindings = new Dictionary<string, IList<PortBinding>>();

                foreach (var portMapping in dockerConfig.Ports)
                {
                    var parts = portMapping.Split(':');
                    if (parts.Length < 2)
                    {
                        _logger.LogWarning("Invalid port mapping format: {PortMapping}. Expected 'hostPort:containerPort'", portMapping);
                        continue;
                    }

                    var hostPort = parts[0];
                    var containerPort = parts[1];

                    var portKey = containerPort.Contains("/") ? containerPort : $"{containerPort}/tcp";
                    exposedPorts[portKey] = default;
                    portBindings[portKey] = new List<PortBinding>
                    {
                        new PortBinding { HostIP = listenAddress, HostPort = hostPort }
                    };

                    _logger.LogDebug("Configuring port binding: {HostPort}:{ContainerPort} (HostIP: {HostIP}, PortKey: {PortKey})",
                        hostPort, containerPort, listenAddress, portKey);
                }

                param.ExposedPorts = exposedPorts;
                hostConfig.PortBindings = portBindings;
                _logger.LogDebug("Set ExposedPorts: {Ports}, PortBindings: {Bindings}",
                    string.Join(",", exposedPorts.Keys), string.Join(",", portBindings.Keys));
            }
            else
            {
                _logger.LogDebug("No ports configured for service '{ServiceName}'", service.Name);
            }
        }
        else
        {
            _logger.LogDebug("Service '{ServiceName}' has ExposureMode={ExposureMode}, skipping port binding", service.Name, service.ExposureMode);
        }

        var mounts = DockerUtils.BuildMounts(service, _volumesOptions.CurrentValue.RootPath);
        if (mounts.Count > 0)
        {
            hostConfig.Mounts = mounts;
            _logger.LogDebug("Configured {MountCount} volume mount(s) for service '{ServiceName}'", mounts.Count, service.Name);
        }

        param.HostConfig = hostConfig;

        if (envVars.Count > 0)
        {
            param.Env = envVars;
        }

        return param;
    }

    private async Task<Result<string>> CreateAndStartContainerAsync(CreateContainerParameters param, Service service, CancellationToken cancellationToken)
    {
        var response = await _dockerClient.Containers.CreateContainerAsync(param, cancellationToken);

        var started = await _dockerClient.Containers.StartContainerAsync(response.ID, new ContainerStartParameters(),
            cancellationToken);

        if (!started)
        {
            _logger.LogError("Failed to start Docker container for service '{ServiceName}'", service.Name);
            return Error.Docker.FailedToStartContainer;
        }

        var environment = service.Environment;
        if (environment != null)
        {
            var environmentNetwork = await _db.Networks
                .FirstOrDefaultAsync(n => n.EnvironmentId == environment.Id, cancellationToken);

            if (environmentNetwork != null)
            {
                var connectResult = await _networkingService.ConnectServiceToNetworksAsync(
                    service.Id,
                    new[] { environmentNetwork.Id },
                    cancellationToken);

                if (connectResult.IsFailure)
                {
                    _logger.LogWarning(
                        "Failed to connect service '{ServiceName}' to environment network, but container is running",
                        service.Name);
                }
            }
        }

        return response.ID;
    }

    private async Task<IList<ContainerListResponse>> GetContainersForServiceAsync(Service service, CancellationToken cancellationToken)
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

        return await _dockerClient.Containers.ListContainersAsync(param, cancellationToken);
    }

    private async Task StopAndRemoveContainersAsync(IList<ContainerListResponse> containers, Service service, string logMessage, CancellationToken cancellationToken)
    {
        await _networkingService.DisconnectServiceFromAllNetworksAsync(service.Id, cancellationToken);
        foreach (var container in containers)
        {
            if (container.State == "running")
            {
                try
                {
                    await _dockerClient.Containers.StopContainerAsync(container.ID, new ContainerStopParameters(), cancellationToken);
                }
                catch (Exception ex) when (ex is TaskCanceledException or OperationCanceledException)
                {
                    _logger.LogDebug("Timeout stopping container '{ContainerId}' for service '{ServiceName}', proceeding with removal", container.ID, service.Name);
                }
            }

            await _dockerClient.Containers.RemoveContainerAsync(container.ID, new ContainerRemoveParameters { Force = true }, cancellationToken);
            _logger.LogInformation(logMessage, container.ID, service.Name);
        }
    }

    private async Task RemoveExistingContainerAsync(Service service, CancellationToken cancellationToken)
    {
        var containers = await GetContainersForServiceAsync(service, cancellationToken);

        if (containers.Count > 0)
        {
            await StopAndRemoveContainersAsync(containers, service, "Removed existing Docker container '{ContainerId}' for service '{ServiceName}' before deploying new version", cancellationToken);
        }
    }
}