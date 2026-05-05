using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Domain;
using Haven.Infrastructure.Persistence.Converters;
using Haven.Infrastructure.Utils;
using Microsoft.Extensions.Logging;

namespace Haven.Infrastructure;

public class EnvironmentVariableSerializer(
    IProjectRepository projectRepository,
    IEnvironmentRepository environmentRepository,
    IServiceRepository serviceRepository,
    IEnvironmentVariableRepository environmentVariableRepository,
    ILogger<EnvironmentVariableSerializer> logger) : IEnvironmentVariableSerializer
{
    public async Task WriteExampleForProjectAsync(Guid projectId, CancellationToken cancellationToken)
    {
        var project = await projectRepository.GetByIdAsync(projectId, cancellationToken);
        if (project is null)
        {
            logger.LogWarning("Project with id {ProjectId} not found. Cannot write example environment variables.", projectId);
            return;
        }

        var envs = await environmentVariableRepository.GetForProjectAsync(projectId, cancellationToken);
        var envList = envs.ToList();
        if (envList.Count == 0)
            return;

        var content = EnvironmentVariableConverter.Convert(envList);
        var path = PathResolver.ProjectEnvExamplePath(project);

        var directory = Path.GetDirectoryName(path);
        if (directory is not null)
            Directory.CreateDirectory(directory);

        await File.WriteAllTextAsync(path, content, cancellationToken);

        logger.LogInformation("Example environment variables written to {Path} for project {ProjectName}", path, project.Name);
    }

    public async Task WriteExampleForEnvironmentAsync(Guid environmentId, CancellationToken cancellationToken)
    {
        var environment = await environmentRepository.GetByIdAsync(environmentId, cancellationToken);
        if (environment is null)
        {
            logger.LogWarning("Environment with id {EnvironmentId} not found. Cannot write example environment variables.", environmentId);
            return;
        }

        var project = await projectRepository.GetByIdAsync(environment.ProjectId, cancellationToken);
        if (project is null)
        {
            logger.LogWarning("Project with id {ProjectId} not found. Cannot write example environment variables.", environment.ProjectId);
            return;
        }

        var envs = await environmentVariableRepository.GetForEnvironmentAsync(environmentId, cancellationToken);
        var envList = envs.ToList();
        if (envList.Count == 0)
            return;

        var content = EnvironmentVariableConverter.Convert(envList);
        var path = PathResolver.EnvironmentEnvExamplePath(project, environment);

        var directory = Path.GetDirectoryName(path);
        if (directory is not null)
            Directory.CreateDirectory(directory);

        await File.WriteAllTextAsync(path, content, cancellationToken);

        logger.LogInformation("Example environment variables written to {Path} for environment {EnvironmentName}", path, environment.Name);
    }

    public async Task WriteExampleForServiceAsync(Guid serviceId, CancellationToken cancellationToken)
    {
        var service = await serviceRepository.GetByIdAsync(serviceId, cancellationToken);
        if (service is null)
        {
            logger.LogWarning("Service with id {ServiceId} not found. Cannot write example environment variables.", serviceId);
            return;
        }

        var environment = await environmentRepository.GetByIdAsync(service.EnvironmentId, cancellationToken);
        if (environment is null)
        {
            logger.LogWarning("Environment with id {EnvironmentId} not found. Cannot write example environment variables.", service.EnvironmentId);
            return;
        }

        var project = await projectRepository.GetByIdAsync(environment.ProjectId, cancellationToken);
        if (project is null)
        {
            logger.LogWarning("Project with id {ProjectId} not found. Cannot write example environment variables.", environment.ProjectId);
            return;
        }

        var envs = await environmentVariableRepository.GetForServiceAsync(serviceId, cancellationToken);
        var envList = envs.ToList();
        if (envList.Count == 0)
            return;

        var content = EnvironmentVariableConverter.Convert(envList);
        var path = PathResolver.ServiceEnvExamplePath(project, environment, service);

        var directory = Path.GetDirectoryName(path);
        if (directory is not null)
            Directory.CreateDirectory(directory);

        await File.WriteAllTextAsync(path, content, cancellationToken);

        logger.LogInformation("Example environment variables written to {Path} for service {ServiceName}", path, service.Name);
    }

    public async Task ReadAndSyncExampleForProjectAsync(Guid projectId, CancellationToken cancellationToken)
    {
        var project = await projectRepository.GetByIdAsync(projectId, cancellationToken);
        if (project is null)
        {
            logger.LogWarning("Project with id {ProjectId} not found. Cannot read example environment variables.", projectId);
            return;
        }

        var path = PathResolver.ProjectEnvExamplePath(project);
        if (!File.Exists(path))
        {
            logger.LogInformation("No example environment file found at {Path} for project {ProjectName}", path, project.Name);
            return;
        }

        var content = await File.ReadAllTextAsync(path, cancellationToken);
        var variables = EnvironmentVariableConverter.Convert(content, projectId, EnvironmentVariableParentType.Project);

        if (variables.Count == 0)
        {
            logger.LogInformation("No environment variables found in {Path} for project {ProjectName}", path, project.Name);
            return;
        }

        await environmentVariableRepository.CleanForProjectAsync(projectId, cancellationToken);
        await environmentVariableRepository.AddAsync(variables, cancellationToken);

        logger.LogInformation("Synced {Count} environment variables from {Path} for project {ProjectName}", variables.Count, path, project.Name);
    }

    public async Task ReadAndSyncExampleForEnvironmentAsync(Guid environmentId, CancellationToken cancellationToken)
    {
        var environment = await environmentRepository.GetByIdAsync(environmentId, cancellationToken);
        if (environment is null)
        {
            logger.LogWarning("Environment with id {EnvironmentId} not found. Cannot read example environment variables.", environmentId);
            return;
        }

        var project = await projectRepository.GetByIdAsync(environment.ProjectId, cancellationToken);
        if (project is null)
        {
            logger.LogWarning("Project with id {ProjectId} not found. Cannot read example environment variables.", environment.ProjectId);
            return;
        }

        var path = PathResolver.EnvironmentEnvExamplePath(project, environment);
        if (!File.Exists(path))
        {
            logger.LogInformation("No example environment file found at {Path} for environment {EnvironmentName}", path, environment.Name);
            return;
        }

        var content = await File.ReadAllTextAsync(path, cancellationToken);
        var variables = EnvironmentVariableConverter.Convert(content, environmentId, EnvironmentVariableParentType.Environment);

        if (variables.Count == 0)
        {
            logger.LogInformation("No environment variables found in {Path} for environment {EnvironmentName}", path, environment.Name);
            return;
        }

        await environmentVariableRepository.CleanForEnvironmentAsync(environmentId, cancellationToken);
        await environmentVariableRepository.AddAsync(variables, cancellationToken);

        logger.LogInformation("Synced {Count} environment variables from {Path} for environment {EnvironmentName}", variables.Count, path, environment.Name);
    }

    public async Task ReadAndSyncExampleForServiceAsync(Guid serviceId, CancellationToken cancellationToken)
    {
        var service = await serviceRepository.GetByIdAsync(serviceId, cancellationToken);
        if (service is null)
        {
            logger.LogWarning("Service with id {ServiceId} not found. Cannot read example environment variables.", serviceId);
            return;
        }

        var environment = await environmentRepository.GetByIdAsync(service.EnvironmentId, cancellationToken);
        if (environment is null)
        {
            logger.LogWarning("Environment with id {EnvironmentId} not found. Cannot read example environment variables.", service.EnvironmentId);
            return;
        }

        var project = await projectRepository.GetByIdAsync(environment.ProjectId, cancellationToken);
        if (project is null)
        {
            logger.LogWarning("Project with id {ProjectId} not found. Cannot read example environment variables.", environment.ProjectId);
            return;
        }

        var path = PathResolver.ServiceEnvExamplePath(project, environment, service);
        if (!File.Exists(path))
        {
            logger.LogInformation("No example environment file found at {Path} for service {ServiceName}", path, service.Name);
            return;
        }

        var content = await File.ReadAllTextAsync(path, cancellationToken);
        var variables = EnvironmentVariableConverter.Convert(content, serviceId, EnvironmentVariableParentType.Service);

        if (variables.Count == 0)
        {
            logger.LogInformation("No environment variables found in {Path} for service {ServiceName}", path, service.Name);
            return;
        }

        await environmentVariableRepository.CleanForServiceAsync(serviceId, cancellationToken);
        await environmentVariableRepository.AddAsync(variables, cancellationToken);

        logger.LogInformation("Synced {Count} environment variables from {Path} for service {ServiceName}", variables.Count, path, service.Name);
    }
}