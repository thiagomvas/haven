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
using Haven.Infrastructure.Utils;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Environment = Haven.Domain.Entities.Environment;
using ServiceStatus = Haven.Domain.ServiceStatus;

namespace Haven.Infrastructure.Deployment;

/// <summary>Deploys services built from a <see cref="DockerfileConfig"/> (raw content or a Git-hosted Dockerfile).</summary>
public class DockerfileDeployService : IDeployService
{
    private readonly ILogger<DockerfileDeployService> _logger;
    private readonly IDockerClient _dockerClient;
    private readonly IDockerContainerRuntime _containerRuntime;
    private readonly INetworkRepository _networkRepository;
    private readonly INetworkingService _networkingService;
    private readonly IEnvironmentVariableService _environmentVariableService;
    private readonly IFeatureFlagService _featureFlagService;
    private readonly IGitService _gitService;
    private readonly IDeploymentLogService _logService;
    private readonly IOptionsMonitor<VolumesOptions> _volumesOptions;
    private readonly IHostPathResolver _hostPathResolver;

    public DockerfileDeployService(
        ILogger<DockerfileDeployService> logger,
        IDockerClient dockerClient,
        IDockerContainerRuntime containerRuntime,
        INetworkRepository networkRepository,
        INetworkingServiceFactory networkingServiceFactory,
        IEnvironmentVariableService environmentVariableService,
        IFeatureFlagService featureFlagService,
        IGitService gitService,
        IDeploymentLogService logService,
        IOptionsMonitor<VolumesOptions> volumesOptions,
        IHostPathResolver hostPathResolver)
    {
        _logger = logger;
        _dockerClient = dockerClient;
        _containerRuntime = containerRuntime;
        _networkRepository = networkRepository;
        _environmentVariableService = environmentVariableService;
        _featureFlagService = featureFlagService;
        _gitService = gitService;
        _logService = logService;
        _volumesOptions = volumesOptions;
        _hostPathResolver = hostPathResolver;
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
                return Error.Git.RepositoryNotFound;
            if (dockerfileConfig == null)
                return Error.InvalidSourceConfig;
        }

        var imageTag = DockerUtils.BuildImageTag(service.Environment?.Project?.Alias, service.Environment?.Alias, service.Alias, service.Id);

        await _containerRuntime.RemoveAllForOwnerAsync(service.Id, _networkingService, "removed before redeploying", cancellationToken);

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
                return Error.Git.RepositoryNotFound;

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

        var param = await BuildCreateContainerParametersAsync(service, imageTag, cancellationToken);

        Result<string> createResult;
        try
        {
            createResult = await _containerRuntime.CreateAndStartAsync(param, cancellationToken);
        }
        catch (DockerApiException ex)
        {
            _logger.LogError(ex, "Failed to create/start Docker container for service '{ServiceName}': {StatusCode} {Message}",
                service.Name, ex.StatusCode, ex.Message);
            await _logService.AppendLogAsync(deploymentId, $"Failed to create/start container: {ex.Message}", cancellationToken);
            return Error.Docker.FailedToStartContainer;
        }

        if (createResult.IsFailure)
        {
            await _logService.AppendLogAsync(deploymentId, "Failed to start container.", cancellationToken);
            return createResult.Error;
        }

        await ConnectToEnvironmentNetworkAsync(service, cancellationToken);

        await _logService.AppendLogAsync(deploymentId, "Container started successfully.", cancellationToken);

        _logger.LogInformation(
            "Successfully deployed service '{ServiceName}' from project '{ProjectName}' from Dockerfile",
            service.Name,
            project.Name);

        var inspect = await _dockerClient.Containers.InspectContainerAsync(createResult.Value, cancellationToken);
        return BuildDeployData(service, param.Name, inspect);
    }

    public async Task<Result> StopAsync(Service service, CancellationToken cancellationToken)
    {
        var containers = await _containerRuntime.GetContainersByLabelAsync(DockerUtils.BuildIdLabel(service.Id), cancellationToken);

        if (containers.Count == 0)
        {
            _logger.LogWarning("No Docker container found for service '{ServiceName}' to stop", service.Name);

            return Error.NotFoundFor("Docker Container", service.Id);
        }

        await _containerRuntime.StopAndRemoveAsync((IReadOnlyCollection<ContainerListResponse>)containers, service.Id, _networkingService,
            "stopped and removed", cancellationToken);

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
            return Error.InvalidSourceConfig;

        var imageTag = DockerUtils.BuildImageTag(service.Environment?.Project?.Alias, service.Environment?.Alias, service.Alias, service.Id);

        _logger.LogInformation(
            "Starting service '{ServiceName}' from project '{ProjectName}'",
            service.Name,
            project.Name);

        var param = await BuildCreateContainerParametersAsync(service, imageTag, cancellationToken);

        Result<string> createResult;
        try
        {
            createResult = await _containerRuntime.CreateAndStartAsync(param, cancellationToken);
        }
        catch (DockerApiException ex)
        {
            _logger.LogError(ex, "Failed to create/start Docker container for service '{ServiceName}': {StatusCode} {Message}",
                service.Name, ex.StatusCode, ex.Message);
            return Error.Docker.FailedToStartContainer;
        }

        if (createResult.IsFailure)
            return createResult.Error;

        await ConnectToEnvironmentNetworkAsync(service, cancellationToken);

        _logger.LogInformation(
            "Successfully started service '{ServiceName}' from project '{ProjectName}'",
            service.Name,
            project.Name);

        var inspect = await _dockerClient.Containers.InspectContainerAsync(createResult.Value, cancellationToken);
        return BuildDeployData(service, param.Name, inspect);
    }

    public async Task CleanupAsync(Service service, CancellationToken cancellationToken)
    {
        await _containerRuntime.RemoveAllForOwnerAsync(service.Id, _networkingService, "cleaned up for deleted service", cancellationToken);

        var imageTag = DockerUtils.BuildImageTag(service.Environment?.Project?.Alias, service.Environment?.Alias, service.Alias, service.Id);
        try
        {
            await _dockerClient.Images.DeleteImageAsync(imageTag, new ImageDeleteParameters { Force = true }, cancellationToken);
            _logger.LogInformation("Removed built image '{ImageTag}' for deleted service '{ServiceName}'", imageTag, service.Name);
        }
        catch
        {
            _logger.LogDebug("Could not remove image '{ImageTag}' for deleted service '{ServiceName}', it may not exist", imageTag, service.Name);
        }
    }

    private async Task<CreateContainerParameters> BuildCreateContainerParametersAsync(Service service, string imageTag, CancellationToken cancellationToken)
    {
        var envs = await _environmentVariableService.BuildVariablesForServiceAsync(service.Id, cancellationToken);
        var flags = await _featureFlagService.GetFlagsAsEnvironmentsForServiceAsync(service.Id, cancellationToken);
        envs.AddRange(flags);

        var volumesRootLocal = Path.GetFullPath(_volumesOptions.CurrentValue.RootPath);
        var volumesRootHost = await _hostPathResolver.ResolveAsync(volumesRootLocal, cancellationToken);
        var mounts = DockerUtils.BuildMounts(service, volumesRootLocal, volumesRootHost);

        _logger.LogDebug("Building container parameters for service '{ServiceName}': ExposureMode={ExposureMode}, MountCount={MountCount}",
            service.Name, service.ExposureMode, mounts.Count);

        var name = DockerUtils.BuildContainerName(service.Environment?.Project?.Alias, service.Environment?.Alias, service.Alias, service.Name, service.Id);
        var labels = DockerUtils.BuildContainerLabels(service);

        // DockerfileConfig has no port-mapping concept today; the shared parameter builder still
        // applies identical LISTEN_ADDRESS/env/mount logic as DockerContainerDeployService.
        return _containerRuntime.BuildContainerParameters(name, labels, imageTag, envs, service.ExposureMode, [], mounts);
    }

    private async Task ConnectToEnvironmentNetworkAsync(Service service, CancellationToken cancellationToken)
    {
        var environment = service.Environment;
        if (environment == null) return;

        var networks = await _networkRepository.GetByProjectAndEnvironmentAsync(environment.ProjectId, environment.Id, cancellationToken);
        var networkId = networks.FirstOrDefault()?.Id;
        if (networkId is null) return;

        await _containerRuntime.ConnectToNetworksAsync(service.Id, [networkId.Value], _networkingService, cancellationToken);
    }

    private static DeployData BuildDeployData(Service service, string containerName, ContainerInspectResponse inspect)
    {
        var rawIp = inspect.NetworkSettings.Networks.Values
            .Select(n => n.IPAddress)
            .FirstOrDefault(ip => !string.IsNullOrEmpty(ip));

        return new DeployData
        {
            ServiceId = service.Id,
            IpAddress = rawIp != null ? IPAddress.Parse(rawIp) : null,
            ContainerName = containerName,
            Ports = inspect.ExtractPortMappings()
        };
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
            return Error.Docker.BuildFailed;
        }

        if (buildErrors.Count > 0)
        {
            _logger.LogError("Docker build failed for image '{ImageTag}' with errors: {Errors}",
                imageTag, string.Join("; ", buildErrors));
            return Error.Docker.BuildFailed;
        }

        _logger.LogInformation("Docker image '{ImageTag}' built successfully", imageTag);
        return Result.Success();
    }
}
