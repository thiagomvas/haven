using Haven.Application.Features.ServiceTemplates.Contracts;
using Haven.Domain.Aggregates;
using Haven.Domain.Entities;
using Haven.Domain.Enums;
using Haven.Domain.ValueObjects;

namespace Haven.Application.Features.ServiceTemplates.Services;

public class ServiceTemplateInstantiator
{
    public Service ConfigureFromTemplate(Service serviceBase, ServiceTemplate template, Dictionary<string, string> inputValues)
    {
        serviceBase.Type = ServiceType.DockerImage;
        serviceBase.ExposureMode = ExposureMode.None;
        serviceBase.SourceConfig = new DockerConfig()
        {
            Image = ResolveVariables(template.Container.DockerImage, inputValues)
        };

        serviceBase.Volumes =
        [
            .. template.Container.Volumes.Select(v => ServiceVolume.Create(
                serviceBase.Id,
                VolumeType.Named,
                $"haven-{serviceBase.Alias}-{serviceBase.Id.ToString("N")[..8]}-{v.Name}",
                v.Mount,
                v.Name,
                true,
                true))
        ];
        return serviceBase;
    }

    public List<Haven.Domain.Entities.EnvironmentVariables> ResolveEnvironmentVariables(Service serviceBase, ServiceTemplate template, Dictionary<string, string> inputValues)
    {
        return template.Container.Env.Select(env => new Haven.Domain.Entities.EnvironmentVariables
        {
            ParentId = serviceBase.Id,
            ParentType = EnvironmentVariableParentType.Service,
            Key = env.Key,
            Value = ResolveVariables(env.Value, inputValues)
        }).ToList();
    }

    public string ResolveVariables(string input, Dictionary<string, string> inputValues)
    {
        foreach (var (key, value) in inputValues)
        {
            input = input.Replace($"{{{{{key}}}}}", value);
        }
        return input;
    }
}