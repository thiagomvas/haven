using Docker.DotNet;
using Docker.DotNet.Models;
using Haven.Application.Common;
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
    private readonly IGitCredentialsRepository _gitCredentialsRepository;

    public DockerfileDeployService(
        ILogger<DockerfileDeployService> logger,
        HavenDbContext db,
        IDockerClient dockerClient,
        INetworkingServiceFactory networkingServiceFactory,
        IEnvironmentVariableService environmentVariableService,
        IFeatureFlagService featureFlagService,
        IGitService gitService,
        IGitCredentialsRepository gitCredentialsRepository)
    {
        _logger = logger;
        _db = db;
        _dockerClient = dockerClient;
        _environmentVariableService = environmentVariableService;
        _featureFlagService = featureFlagService;
        _gitService = gitService;
        _gitCredentialsRepository = gitCredentialsRepository;
        _networkingService = networkingServiceFactory.Create(ServiceType.DockerImage) ?? throw new InvalidOperationException("No networking service found for Docker networking");
    }

    public ServiceType ServiceType => ServiceType.Dockerfile;

    public async Task<Result> DeployAsync(Service service, CancellationToken cancellationToken)
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

        var imageTag = DockerUtils.BuildImageTag(service.Id);

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
                var cloneResult = await _gitService.CloneServiceRepositoryAsync(
                    service.Id,
                    dockerfileConfig.Repository!,
                    cancellationToken);

                if (cloneResult.IsFailure)
                {
                    _logger.LogError("Failed to clone repository '{Repository}' for service '{ServiceName}'", dockerfileConfig.Repository, service.Name);
                    return cloneResult;
                }
            }
            else
            {
                var pullResult = await _gitService.PullServiceRepositoryAsync(
                    service.Id,
                    dockerfileConfig.Branch ?? "main",
                    cancellationToken);

                if (pullResult.IsFailure)
                {
                    _logger.LogWarning("Failed to pull latest changes for service '{ServiceName}', proceeding with existing code", service.Name);
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

            var buildResult = await WaitForImageBuildAsync(
                _dockerClient,
                buildParams,
                buildContext,
                imageTag,
                cancellationToken);

            if (buildResult.IsFailure)
                return buildResult;
        }

        _logger.LogInformation(
            "Deploying service '{ServiceName}' from project '{ProjectName}' from Dockerfile",
            service.Name,
            project.Name);

        var envs = await _environmentVariableService.BuildVariablesForServiceAsync(service.Id, cancellationToken);
        var flags = await _featureFlagService.GetFlagsAsEnvironmentsForServiceAsync(service.Id, cancellationToken);
        envs.AddRange(flags);

        var param = BuildCreateContainerParameters(service, imageTag, envs.ToList());
        var result = await CreateAndStartContainerAsync(param, service, cancellationToken);

        if (result.IsFailure)
            return result;

        service.MarkDeployed();
        await _db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation(
            "Successfully deployed service '{ServiceName}' from project '{ProjectName}' from Dockerfile",
            service.Name,
            project.Name);
        return Result.Success();
    }

    public async Task<Result> StopAsync(Service service, CancellationToken cancellationToken)
    {
        var containers = await GetContainersForServiceAsync(service, cancellationToken);

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

        await StopAndRemoveContainersAsync(containers, service, "Stopped and removed Docker container '{ContainerId}' for service '{ServiceName}'", cancellationToken);

        return Result.Success();
    }

    public async Task<Result> RestartAsync(Service service, CancellationToken cancellationToken)
    {
        var environment = service.Environment;
        if (environment == null) return Error.NotFoundFor(nameof(Environment), service.EnvironmentId);
        var project = environment.Project;
        if (project == null) return Error.NotFoundFor(nameof(Project), environment.ProjectId);

        var dockerfileConfig = service.SourceConfig as DockerfileConfig;
        if (dockerfileConfig == null)
            return Error.Validation;

        var imageTag = DockerUtils.BuildImageTag(service.Id);

        _logger.LogInformation(
            "Restarting service '{ServiceName}' from project '{ProjectName}'",
            service.Name,
            project.Name);

        if (dockerfileConfig.Source == DockerfileSource.Git)
        {
            var pullResult = await _gitService.PullServiceRepositoryAsync(
                service.Id,
                dockerfileConfig.Branch ?? "main",
                cancellationToken);

            if (pullResult.IsFailure)
            {
                _logger.LogWarning("Failed to pull latest changes for service '{ServiceName}', proceeding with existing code", service.Name);
            }

            var repoPath = _gitService.GetServiceRepositoryPath(service.Id);
            if (string.IsNullOrWhiteSpace(repoPath))
                return Error.Validation;

            try
            {
                await _dockerClient.Images.DeleteImageAsync(imageTag, new ImageDeleteParameters { Force = true }, cancellationToken);
            }
            catch
            {
                _logger.LogDebug("Could not remove old image '{ImageTag}', proceeding with build", imageTag);
            }

            var buildContext = await DockerUtils.CreateTarArchiveFromDirectoryAsync(repoPath, cancellationToken);
            var dockerfilePath = dockerfileConfig.FilePath ?? "Dockerfile";

            using (buildContext)
            {
                var buildParams = new ImageBuildParameters
                {
                    Tags = [imageTag],
                    Dockerfile = dockerfilePath,
                    Labels = DockerUtils.BuildContainerLabels(service)
                };

                var buildResult = await WaitForImageBuildAsync(
                    _dockerClient,
                    buildParams,
                    buildContext,
                    imageTag,
                    cancellationToken);

                if (buildResult.IsFailure)
                    return buildResult;
            }
        }

        await RemoveExistingContainerAsync(service, cancellationToken);

        var envs = await _environmentVariableService.BuildVariablesForServiceAsync(service.Id, cancellationToken);
        var flags = await _featureFlagService.GetFlagsAsEnvironmentsForServiceAsync(service.Id, cancellationToken);
        envs.AddRange(flags);

        var param = BuildCreateContainerParameters(service, imageTag, envs.ToList());
        var result = await CreateAndStartContainerAsync(param, service, cancellationToken);

        if (result.IsFailure)
            return result;

        project.RestartService(service.EnvironmentId, service.Id);
        await _db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation(
            "Successfully restarted service '{ServiceName}' from project '{ProjectName}'",
            service.Name,
            project.Name);

        return Result.Success();
    }

    private CreateContainerParameters BuildCreateContainerParameters(Service service, string imageTag, List<EnvironmentVariables>? envs = null)
    {
        var param = new CreateContainerParameters()
        {
            Name = DockerUtils.BuildContainerName(service.Name, service.Id),
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

    private async Task<Result> CreateAndStartContainerAsync(CreateContainerParameters param, Service service, CancellationToken cancellationToken)
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

        return Result.Success();
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
        CancellationToken cancellationToken)
    {
        var buildErrors = new List<string>();
        var lastStreamOutput = string.Empty;

        var progress = new Progress<JSONMessage>(message =>
        {
            if (message.Stream != null)
            {
                lastStreamOutput = message.Stream;
                _logger.LogDebug("Docker build output: {Output}", message.Stream.TrimEnd());
            }

            if (message.Error != null)
            {
                _logger.LogError("Docker build error: {Error}", message.ErrorMessage);
                buildErrors.Add(message.ErrorMessage);
            }

            if (message.ErrorMessage != null)
            {
                _logger.LogError("Docker build error detail: {Error}", message.ErrorMessage);
                buildErrors.Add(message.ErrorMessage);
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