using Haven.Application.Features.ServiceTemplates.Contracts;
using Haven.Application.Features.ServiceTemplates.Services;
using Haven.Domain.Aggregates;
using Haven.Domain.Enums;
using Haven.Domain.ValueObjects;

using Shouldly;

namespace Haven.Application.Tests.Features.ServiceTemplates.Services;

[TestFixture]
[Category("Unit")]
public class ServiceTemplateInstantiatorTests
{
    private ServiceTemplateInstantiator _sut;

    [SetUp]
    public void Setup()
    {
        _sut = new ServiceTemplateInstantiator();
    }
    
    [Test]
    public void Configure_ShouldMapVolumesAndResolveVariables()
    {
        var serviceBase = CreateServiceBase();
        var template = new ServiceTemplate
        {
            Container = new ServiceTemplateContainer
            {
                DockerImage = "my-image:{{version}}",
                Volumes = new List<ServiceTemplateContainerVolume>
                {
                    new ServiceTemplateContainerVolume { Name = "data", Mount = "/data" }
                }
            }
        };
        var inputValues = new Dictionary<string, string>
        {
            { "version", "1.0.0" }
        };

        var configuredService = _sut.ConfigureFromTemplate(serviceBase, template, inputValues);

        configuredService.Volumes.Count.ShouldBe(template.Container.Volumes.Count);
        configuredService.SourceConfig.ShouldBeOfType<DockerConfig>();
        configuredService.SourceConfig.ShouldNotBeNull();
        var dockerConfig = (DockerConfig)configuredService.SourceConfig;
        dockerConfig.Image.ShouldBe("my-image:1.0.0");
        
        var volumes = configuredService.Volumes.ToList();
        volumes[0].Name.ShouldContain("data");
        volumes[0].Name.ShouldContain("haven");
        volumes[0].Target.ShouldBe("/data");
        volumes[0].Type.ShouldBe(VolumeType.Named);
        volumes[0].Source.ShouldBe("data");
    }

    [Test]
    public void Configure_ShouldMapDockerImageAndResolveVariables()
    {
        var serviceBase = CreateServiceBase();
        var template = new ServiceTemplate
        {
            Container = new ServiceTemplateContainer
            {
                DockerImage = "my-image:{{version}}"
            }
        };
        var inputValues = new Dictionary<string, string>
        {
            { "version", "1.0.0" }
        };

        var configuredService = _sut.ConfigureFromTemplate(serviceBase, template, inputValues);

        configuredService.SourceConfig.ShouldBeOfType<DockerConfig>();
        configuredService.SourceConfig.ShouldNotBeNull();
        var dockerConfig = (DockerConfig)configuredService.SourceConfig;
        dockerConfig.Image.ShouldBe("my-image:1.0.0");
        
    }
    
    [Test]
    public void ResolveEnvironmentVariables_ShouldResolveVariablesAndSetParent()
    {
        var serviceBase = CreateServiceBase();
        var template = new ServiceTemplate
        {
            Container = new ServiceTemplateContainer
            {
                DockerImage = "postgres:{{version}}-alpine",
                Env = new Dictionary<string, string>
                {
                    { "POSTGRES_USER", "{{username}}" },
                    { "POSTGRES_PASSWORD", "{{password}}" },
                    { "POSTGRES_DB", "{{database}}" }
                }
            }
        };
        var inputValues = new Dictionary<string, string>
        {
            { "version", "17" },
            { "username", "admin" },
            { "password", "secret" },
            { "database", "app" }
        };

        var environmentVariables = _sut.ResolveEnvironmentVariables(serviceBase, template, inputValues).ToList();

        environmentVariables.Count.ShouldBe(3);
        environmentVariables.ShouldAllBe(v => v.ParentId == serviceBase.Id);
        environmentVariables.ShouldAllBe(v => v.ParentType == EnvironmentVariableParentType.Service);
        environmentVariables.Single(v => v.Key == "POSTGRES_USER").Value.ShouldBe("admin");
        environmentVariables.Single(v => v.Key == "POSTGRES_PASSWORD").Value.ShouldBe("secret");
        environmentVariables.Single(v => v.Key == "POSTGRES_DB").Value.ShouldBe("app");
    }

    private static Service CreateServiceBase()
    {
        return Service.Create(Guid.NewGuid(), "test-service", ServiceType.DockerImage, ExposureMode.None);
    }
}