using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Domain;
using Haven.Domain.Entities;
using Haven.Infrastructure.Persistence.Converters;

namespace Haven.Infrastructure.Services;

public class EnvironmentVariableService(
    IProjectRepository projectRepository,
    IEnvironmentRepository environmentRepository,
    IServiceRepository serviceRepository,
    IEnvironmentVariableRepository environmentVariableRepository) : IEnvironmentVariableService
{
    public async Task<IEnumerable<EnvironmentVariables>> BuildVariablesForServiceAsync(Guid serviceId,
        CancellationToken cancellationToken)
    {
        var service = await serviceRepository.GetByIdAsync(serviceId, cancellationToken);
        if (service is null || service.Environment is null || service.Environment.Project is null)
        {
            return [];
        }

        var environmentEnvs = await BuildVariablesForEnvironmentAsync(service.Environment.Id, cancellationToken);
        var serviceEnvs = await environmentVariableRepository.GetForServiceAsync(serviceId, cancellationToken);
        return Merge(environmentEnvs, serviceEnvs);
    }

    public async Task<IEnumerable<EnvironmentVariables>> BuildVariablesForEnvironmentAsync(Guid environmentId,
        CancellationToken cancellationToken)
    {
        var environment = await environmentRepository.GetByIdAsync(environmentId, cancellationToken);
        if (environment is null || environment.Project is null) return [];

        var projectEnvs = await BuildVariablesForProjectAsync(environment.Project.Id, cancellationToken);
        var environmentEnvs =
            await environmentVariableRepository.GetForEnvironmentAsync(environmentId, cancellationToken);
        return Merge(projectEnvs, environmentEnvs);
    }

    public async Task<IEnumerable<EnvironmentVariables>> BuildVariablesForProjectAsync(Guid projectId,
        CancellationToken cancellationToken)
    {
        var project = await projectRepository.GetByIdAsync(projectId, cancellationToken);
        if (project is null)
            return [];

        var envs = await environmentVariableRepository.GetForProjectAsync(project.Id, cancellationToken);
        return envs;
    }

    public async Task<string> BuildEnvFileForServiceAsync(Guid serviceId, CancellationToken cancellationToken)
    {
        var envs = await BuildVariablesForServiceAsync(serviceId, cancellationToken);
        var content = EnvironmentVariableConverter.Convert(envs);

        return content;
    }

    public async Task<string> BuildEnvFileForEnvironmentAsync(Guid environmentId, CancellationToken cancellationToken)
    {
        var envs = await BuildVariablesForEnvironmentAsync(environmentId, cancellationToken);
        var content = EnvironmentVariableConverter.Convert(envs);

        return content;
    }

    public async Task<string> BuildEnvFileForServiceDirectAsync(Guid serviceId, CancellationToken cancellationToken)
    {
        var envs = await environmentVariableRepository.GetForServiceAsync(serviceId, cancellationToken);
        var content = EnvironmentVariableConverter.Convert(envs);

        return content;
    }

    public async Task<string> BuildEnvFileForEnvironmentDirectAsync(Guid environmentId,
        CancellationToken cancellationToken)
    {
        var envs = await environmentVariableRepository.GetForEnvironmentAsync(environmentId, cancellationToken);
        var content = EnvironmentVariableConverter.Convert(envs);

        return content;
    }

    public async Task<string> BuildEnvFileForProjectAsync(Guid projectId, CancellationToken cancellationToken)
    {
        var envs = await BuildVariablesForProjectAsync(projectId, cancellationToken);
        var content = EnvironmentVariableConverter.Convert(envs);

        return content;
    }

    public async Task SetEnvironmentVariablesFromFileForProjectAsync(Guid projectId, string content,
        CancellationToken cancellationToken)
    {
        var envs = EnvironmentVariableConverter.Convert(content, projectId, EnvironmentVariableParentType.Project);

        await environmentVariableRepository.CleanForProjectAsync(projectId, cancellationToken);
        await environmentVariableRepository.AddAsync(envs, cancellationToken);
    }

    public async Task SetEnvironmentVariablesFromFileForEnvironmentAsync(Guid environmentId, string content,
        CancellationToken cancellationToken)
    {
        var envs = EnvironmentVariableConverter.Convert(content, environmentId, EnvironmentVariableParentType.Environment);

        await environmentVariableRepository.CleanForEnvironmentAsync(environmentId, cancellationToken);
        await environmentVariableRepository.AddAsync(envs, cancellationToken);
    }

    public async Task SetEnvironmentVariablesFromFileForServiceAsync(Guid serviceId, string content,
        CancellationToken cancellationToken)
    {
        var envs = EnvironmentVariableConverter.Convert(content, serviceId, EnvironmentVariableParentType.Service);

        await environmentVariableRepository.CleanForServiceAsync(serviceId, cancellationToken);
        await environmentVariableRepository.AddAsync(envs, cancellationToken);
    }

    private static IEnumerable<EnvironmentVariables> Merge(IEnumerable<EnvironmentVariables> @base,
        IEnumerable<EnvironmentVariables> overrides)
    {
        var dict = @base.ToDictionary(x => x.Key, x => x);

        foreach (var dest in overrides)
        {
            dict[dest.Key] = dest;
        }

        return dict.Values;
    }
}