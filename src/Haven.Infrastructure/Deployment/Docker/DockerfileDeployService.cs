using System.Net;
using Docker.DotNet;
using Docker.DotNet.Models;

using Haven.Application.Common;
using Haven.Application.Common.Contracts;
using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Interfaces.Deployment;
using Haven.Application.Common.Interfaces.Repositories;
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

public class DockerfileDeployService : IDeployService
{
    private readonly ILogger<DockerfileDeployService> _logger;
    private readonly HavenDbContext _db;
    private readonly IDockerClient _dockerClient;
    private readonly INetworkingService _networkingService;
    private readonly IEnvironmentVariableService _environmentVariableService;
    private readonly IFeatureFlagService _featureFlagService;
    private readonly IGitService _gitService;
    private readonly IDeploymentLogService _logService;

    public DockerfileDeployService(
        ILogger<DockerfileDeployService> logger,
        IDockerClient dockerClient,
        INetworkingServiceFactory networkingServiceFactory,
        IEnvironmentVariableService environmentVariableService,
        IFeatureFlagService featureFlagService,
        IGitService gitService,
        IDeploymentLogService logService,
        HavenDbContext db)
    {
        _logger = logger;
        _dockerClient = dockerClient;
        _environmentVariableService = environmentVariableService;
        _featureFlagService = featureFlagService;
        _gitService = gitService;
        _logService = logService;
        _db = db;
        _networkingService = networkingServiceFactory.Create(ServiceType.DockerImage) ?? throw new InvalidOperationException("No networking service found for Docker networking");
    }

    public ServiceType ServiceType => ServiceType.Dockerfile;

    public async Task<Result<DeployData>> DeployAsync(Service service, Guid deploymentId, CancellationToken cancellationToken)
    {
        var environment = service.Environment;
        if (environment == null) return Error.NotFoundFor(nameof(Environment), service.EnvironmentId);
        var project = environment.Project;
        if (project == null) return Error.NotFoundFor(nameof(Project), environment.ProjectId);

        var dockerfileConfig = service.SourceConfig as DockerfileConfig;
        if (dockerfileConfig == null || string.IsNullOrWhiteSpace(dockerfileConfig.Content) && dockerfileConfig.Source == DockerfileSource.Raw)
        {
            if (dockerfileConfig?.Source == DockerfileSource.Git && string.IsNullOrWhiteSpace(dockerfileConfig.Repository))
                return Error.Validation;
            if (dockerfileConfig == null)
                return Error.Validation;
        }

        var imageTag = DockerUtils.BuildImageTag(service.Environment?.Project?.Alias, service.Environment?.Alias, service.Alias, service.Id);

        await _networkingService.DisconnectServiceFromAllNetworksAsync(service.Id, cancellationToken);
        await RemoveExistingContainerAsync(service, cancellationToken);

        _logger.LogInformation(
            "Building Docker image '{ImageTag}' for service '{ServiceName}' from project '{ProjectName}'",
            imageTag,
            service.Name,
            project.Name);

        try
        {
            await _dockerClient.Images.DeleteImageAsync(imageTag, new ImageDeleteParameters { Force = true }, cancellationToken);
        }
        catch
        {
            _logger.LogDebug("Could not remove old image '{ImageTag}', proceeding with build", imageTag);
        }

        Stream buildContext;
        string dockerfilePath;

        if (dockerfileConfig.Source == DockerfileSource.Git)
        {
            var repoExists = _gitService.ServiceRepositoryExists(service.Id);
            if (!repoExists)
            {
                await _logService.AppendLogAsync(deploymentId, $"Cloning repository '{dockerfileConfig.Repository}'...", cancellationToken);
                var cloneResult = await _gitService.CloneServiceRepositoryAsync(
                    service.Id,
                    dockerfileConfig.Repository!,
                    cancellationToken);

                if (cloneResult.IsFailure)
                {
                    _logger.LogError("Failed to clone repository '{Repository}' for service '{ServiceName}'", dockerfileConfig.Repository, service.Name);
                    await _logService.AppendLogAsync(deploymentId, $"Failed to clone repository '{dockerfileConfig.Repository}'.", cancellationToken);
                    return cloneResult.Error;
                }

                await _logService.AppendLogAsync(deploymentId, "Repository cloned successfully.", cancellationToken);
            }
            else
            {
                await _logService.AppendLogAsync(deploymentId, $"Pulling latest changes from '{dockerfileConfig.Branch ?? "main"}'...", cancellationToken);
                var pullResult = await _gitService.PullServiceRepositoryAsync(
                    service.Id,
                    dockerfileConfig.Branch ?? "main",
                    cancellationToken);

                if (pullResult.IsFailure)
                {
                    _logger.LogWarning("Failed to pull latest changes for service '{ServiceName}', proceeding with existing code", service.Name);
                    await _logService.AppendLogAsync(deploymentId, "Failed to pull latest changes, proceeding with existing code.", cancellationToken);
                }
                else
                {
                    await _logService.AppendLogAsync(deploymentId, "Repository updated successfully.", cancellationToken);
                }
            }

            var repoPath = _gitService.GetServiceRepositoryPath(service.Id);
            if (string.IsNullOrWhiteSpace(repoPath))
                return Error.Validation;

            buildContext = await DockerUtils.CreateTarArchiveFromDirectoryAsync(repoPath, cancellationToken);
            dockerfilePath = dockerfileConfig.FilePath ?? "Dockerfile";
        }
        else
        {
            buildContext = await DockerUtils.CreateTarArchiveFromContentAsync(dockerfileConfig.Content!, cancellationToken);
            dockerfilePath = "Dockerfile";
        }

        using (buildContext)
        {
            var buildParams = new ImageBuildParameters
            {
                Tags = [imageTag],
                Dockerfile = dockerfilePath,
                Labels = DockerUtils.BuildContainerLabels(service)
            };

            _logger.LogInformation("Starting docker build for image '{ImageTag}'", imageTag);
            await _logService.AppendLogAsync(deploymentId, $"Building image '{imageTag}'...", cancellationToken);

            var buildResult = await WaitForImageBuildAsync(
                _dockerClient,
                buildParams,
                buildContext,
                imageTag,
                deploymentId,
                cancellationToken);

            if (buildResult.IsFailure)
                return buildResult.Error;
        }

        await _logService.AppendLogAsync(deploymentId, "Image built successfully. Creating container...", cancellationToken);

        _logger.LogInformation(
            "Deploying service '{ServiceName}' from project '{ProjectName}' from Dockerfile",
            service.Name,
            project.Name);

        var envs = await _environmentVariableService.BuildVariablesForServiceAsync(service.Id, cancellationToken);
        var flags = await _featureFlagService.GetFlagsAsEnvironmentsForServiceAsync(service.Id, cancellationToken);
        envs.AddRange(flags);

        var param = BuildCreateContainerParameters(service, imageTag, envs.ToList());
        var createResult = await CreateAndStartContainerAsync(param, service, cancellationToken);

        if (createResult.IsFailure)
        {
            await _logService.AppendLogAsync(deploymentId, "Failed to start container.", cancellationToken);
            return createResult.Error;
        }

        await _logService.AppendLogAsync(deploymentId, "Container started successfully.", cancellationToken);

        _logger.LogInformation(
            "Successfully deployed service '{ServiceName}' from project '{ProjectName}' from Dockerfile",
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

        var dockerfileConfig = service.SourceConfig as DockerfileConfig;
        if (dockerfileConfig == null)
            return Error.Validation;

        var imageTag = DockerUtils.BuildImageTag(service.Environment?.Project?.Alias, service.Environment?.Alias, service.Alias, service.Id);

        _logger.LogInformation(
            "Starting service '{ServiceName}' from project '{ProjectName}'",
            service.Name,
            project.Name);

        var envs = await _environmentVariableService.BuildVariablesForServiceAsync(service.Id, cancellationToken);
        var flags = await _featureFlagService.GetFlagsAsEnvironmentsForServiceAsync(service.Id, cancellationToken);
        envs.AddRange(flags);

        var param = BuildCreateContainerParameters(service, imageTag, envs.ToList());
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

    private CreateContainerParameters BuildCreateContainerParameters(Service service, string imageTag, List<EnvironmentVariables>? envs = null)
    {
        var param = new CreateContainerParameters()
        {
            Name = DockerUtils.BuildContainerName(service.Environment?.Project?.Alias, service.Environment?.Alias, service.Alias, service.Name, service.Id),
            Labels = DockerUtils.BuildContainerLabels(service),
            Image = imageTag,
        };

        var envVars = (envs ?? []).Select(e => $"{e.Key}={e.Value}").ToList();

        _logger.LogDebug("Building container parameters for service '{ServiceName}': ExposureMode={ExposureMode}",
            service.Name, service.ExposureMode);

        if (service.ExposureMode is ExposureMode.Internal or ExposureMode.External)
        {
            var listenAddress = service.ExposureMode == ExposureMode.Internal ? "127.0.0.1" : "0.0.0.0";
            envVars.Add($"LISTEN_ADDRESS={listenAddress}");
        }

        if (envVars.Count > 0)
        {
            param.Env = envVars;
        }

        return param;
    }

    private async Task<Result<string>> CreateAndStartContainerAsync(CreateContainerParameters param, Service service, CancellationToken cancellationToken)
    {
        var response = await _dockerClient.Containers.CreateContainerAsync(param, cancellationToken);

        var started = await _dockerClient.Containers.StartContainerAsync(response.ID, new ContainerStartParameters(), cancellationToken);

        if (!started)
        {
            _logger.LogError("Failed to start Docker container for service '{ServiceName}'", service.Name);
            return Error.Validation;
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

    private async Task<Result> WaitForImageBuildAsync(
        IDockerClient dockerClient,
        ImageBuildParameters buildParams,
        Stream buildContext,
        string imageTag,
        Guid deploymentId,
        CancellationToken cancellationToken)
    {
        var buildErrors = new List<string>();

        var progress = new Progress<JSONMessage>(message =>
        {
            if (message.Stream != null)
            {
                var line = message.Stream.TrimEnd();
                if (!string.IsNullOrEmpty(line))
                {
                    _logger.LogDebug("Docker build output: {Output}", line);
                    _ = _logService.AppendLogAsync(deploymentId, line, cancellationToken);
                }
            }

            if (message.Error != null)
            {
                _logger.LogError("Docker build error: {Error}", message.ErrorMessage);
                buildErrors.Add(message.ErrorMessage);
                _ = _logService.AppendLogAsync(deploymentId, $"ERROR: {message.ErrorMessage}", cancellationToken);
            }
        });

        try
        {
            await dockerClient.Images.BuildImageFromDockerfileAsync(
                buildParams,
                buildContext,
                null,
                null,
                progress,
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Docker build failed for image '{ImageTag}'", imageTag);
            await _logService.AppendLogAsync(deploymentId, $"Docker build failed: {ex.Message}", cancellationToken);
            return Error.Validation;
        }

        if (buildErrors.Count > 0)
        {
            _logger.LogError("Docker build failed for image '{ImageTag}' with errors: {Errors}",
                imageTag, string.Join("; ", buildErrors));
            return Error.Validation;
        }

        _logger.LogInformation("Docker image '{ImageTag}' built successfully", imageTag);
        return Result.Success();
    }
}