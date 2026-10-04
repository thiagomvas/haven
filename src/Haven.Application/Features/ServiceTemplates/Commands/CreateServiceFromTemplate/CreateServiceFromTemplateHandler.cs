using Haven.Application.Common;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Interfaces.Services;
using Haven.Application.Common.Messaging;
using Haven.Application.Features.ServiceTemplates.Contracts;
using Haven.Application.Features.ServiceTemplates.Services;
using Haven.Domain.Aggregates;
using Haven.Domain.Enums;
using Haven.Domain.ValueObjects;

using Environment = Haven.Domain.Aggregates.Environment;

namespace Haven.Application.Features.ServiceTemplates.Commands.CreateServiceFromTemplate;

public sealed class CreateServiceFromTemplateHandler(
    IProjectRepository projectRepository,
    IServiceRepository serviceRepository,
    IServiceTemplateRepository templateRepository,
    IEnvironmentVariableRepository environmentVariableRepository,
    ISecretVariableRepository secretVariableRepository,
    IManagedVolumeFileService managedVolumeFileService,
    ServiceTemplateInstantiator instantiator)
    : ICommandHandler<CreateServiceFromTemplateCommand, Guid>
{
    public async ValueTask<Result<Guid>> Handle(CreateServiceFromTemplateCommand request, CancellationToken cancellationToken)
    {
        var template = await templateRepository.GetByIdAsync(request.TemplateId, cancellationToken);
        if (template is null)
            return Error.NotFoundFor(nameof(ServiceTemplate), request.TemplateId);

        if (string.IsNullOrWhiteSpace(template.Container?.DockerImage))
            return Error.InvalidOperation($"Template '{template.Id}' is missing a container image and cannot be instantiated.");

        var project = await projectRepository.GetByIdAsync(request.ProjectId, cancellationToken);
        if (project is null)
            return Error.NotFoundFor(nameof(Project), request.ProjectId);

        var environment = project.Environments.FirstOrDefault(e => e.Id == request.EnvironmentId);
        if (environment is null)
            return Error.NotFoundFor(nameof(Environment), request.EnvironmentId);

        var name = string.IsNullOrWhiteSpace(request.Name) ? template.Name : request.Name.Trim();

        if (environment.Services.Any(s => string.Equals(s.Name, name, StringComparison.OrdinalIgnoreCase)))
            return Error.ConflictFor("Service", name);

        if (!string.IsNullOrEmpty(request.Alias) && environment.Services.Any(s => string.Equals(s.Alias, request.Alias, StringComparison.OrdinalIgnoreCase)))
            return Error.ConflictFor("Service alias", request.Alias);

        var resolveResult = ResolveInputValues(template, request.InputValues);
        if (resolveResult.IsFailure)
            return Result<Guid>.Failure(resolveResult.Error);

        var service = project.AddService(request.EnvironmentId, name, ServiceType.DockerImage, request.ExposureMode, request.Alias);
        service = instantiator.ConfigureFromTemplate(service, template, resolveResult.Value);
        if (service.SourceConfig is DockerConfig dockerConfig)
        {
            dockerConfig.Ports = request.Ports;
            service.SourceConfig = dockerConfig;
        }
        var resolvedVariables = instantiator.ResolveEnvironmentVariables(service, template, resolveResult.Value);
        var seedFiles = instantiator.ResolveManagedVolumeSeedFiles(service, template, resolveResult.Value);

        var computedPropertiesResult = instantiator.AddComputedProperties(service, template, resolveResult.Value);
        if (computedPropertiesResult.IsFailure)
            return Result<Guid>.Failure(computedPropertiesResult.Error);

        var actionsResult = instantiator.AddCustomActions(service, template, resolveResult.Value);
        if (actionsResult.IsFailure)
            return Result<Guid>.Failure(actionsResult.Error);

        await serviceRepository.AddAsync(service, cancellationToken);

        if (resolvedVariables.EnvironmentVariables.Count > 0)
            await environmentVariableRepository.AddAsync(resolvedVariables.EnvironmentVariables, cancellationToken);

        foreach (var secret in resolvedVariables.Secrets)
            await secretVariableRepository.AddAsync(secret, cancellationToken);

        foreach (var seedFile in seedFiles)
        {
            var writeResult = await managedVolumeFileService.WriteFileAsync(
                service.Id, seedFile.VolumeId, seedFile.RelativePath, seedFile.Content, cancellationToken);
            if (writeResult.IsFailure)
                return Result<Guid>.Failure(writeResult.Error);
        }

        return Result<Guid>.CreatedFor(service.Id);
    }

    private static Result<Dictionary<string, string>> ResolveInputValues(
        ServiceTemplate template, Dictionary<string, string> provided)
    {
        var resolved = new Dictionary<string, string>();

        foreach (var field in template.Inputs)
        {
            var hasValue = provided.TryGetValue(field.Key, out var value) && !string.IsNullOrEmpty(value);

            if (!hasValue)
            {
                if (!string.IsNullOrEmpty(field.DefaultValue))
                {
                    resolved[field.Key] = field.DefaultValue;
                    continue;
                }

                return Error.Validation($"Input '{field.Key}' is required.");
            }

            if (field.Options is { Length: > 0 } && !field.Options.Contains(value))
                return Error.Validation($"Input '{field.Key}' must be one of: {string.Join(", ", field.Options)}.");

            resolved[field.Key] = value!;
        }

        return resolved;
    }
}