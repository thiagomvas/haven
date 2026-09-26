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
                DockerImage = "my-image:${{ inputs.version }}",
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
        volumes[0].ReadOnly.ShouldBeFalse();
    }

    [Test]
    public void Configure_ShouldMapDockerImageAndResolveVariables()
    {
        var serviceBase = CreateServiceBase();
        var template = new ServiceTemplate
        {
            Container = new ServiceTemplateContainer
            {
                DockerImage = "my-image:${{ inputs.version }}"
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
    public void Configure_ShouldResolveCommandArgs()
    {
        var serviceBase = CreateServiceBase();
        var template = new ServiceTemplate
        {
            Container = new ServiceTemplateContainer
            {
                DockerImage = "redis:${{ inputs.version }}-alpine",
                CommandArgs = new List<string> { "--requirepass", "${{ inputs.password }}" }
            }
        };
        var inputValues = new Dictionary<string, string>
        {
            { "version", "7" },
            { "password", "s3cret" }
        };

        var configuredService = _sut.ConfigureFromTemplate(serviceBase, template, inputValues);

        var dockerConfig = (DockerConfig)configuredService.SourceConfig!;
        dockerConfig.CommandArgs.ShouldBe(["--requirepass", "s3cret"]);
    }

    [Test]
    public void ResolveVariables_ShouldSupportNoWhitespaceVariant()
    {
        var result = _sut.ResolveVariables("image:${{inputs.version}}", new Dictionary<string, string>
        {
            { "version", "2.0.0" }
        });

        result.ShouldBe("image:2.0.0");
    }

    [Test]
    public void ResolveVariables_ShouldLeaveUnknownPlaceholderUntouched()
    {
        var result = _sut.ResolveVariables("image:${{ inputs.unknown }}", new Dictionary<string, string>());

        result.ShouldBe("image:${{ inputs.unknown }}");
    }

    [Test]
    public void ResolveEnvironmentVariables_ShouldResolveVariablesAndSetParent()
    {
        var serviceBase = CreateServiceBase();
        var template = new ServiceTemplate
        {
            Inputs = new List<TemplateInputField>
            {
                new() { Key = "username", Type = TemplateInputFieldType.Text },
                new() { Key = "password", Type = TemplateInputFieldType.Secret },
                new() { Key = "database", Type = TemplateInputFieldType.Text }
            },
            Container = new ServiceTemplateContainer
            {
                DockerImage = "postgres:${{ inputs.version }}-alpine",
                Env = new Dictionary<string, string>
                {
                    { "POSTGRES_USER", "${{ inputs.username }}" },
                    { "POSTGRES_PASSWORD", "${{ inputs.password }}" },
                    { "POSTGRES_DB", "${{ inputs.database }}" }
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

        var resolved = _sut.ResolveEnvironmentVariables(serviceBase, template, inputValues);

        resolved.EnvironmentVariables.Count.ShouldBe(2);
        resolved.EnvironmentVariables.ShouldAllBe(v => v.ParentId == serviceBase.Id);
        resolved.EnvironmentVariables.ShouldAllBe(v => v.ParentType == EnvironmentVariableParentType.Service);
        resolved.EnvironmentVariables.Single(v => v.Key == "POSTGRES_USER").Value.ShouldBe("admin");
        resolved.EnvironmentVariables.Single(v => v.Key == "POSTGRES_DB").Value.ShouldBe("app");

        resolved.Secrets.Count.ShouldBe(1);
        var passwordSecret = resolved.Secrets.Single(v => v.Key == "POSTGRES_PASSWORD");
        passwordSecret.ParentId.ShouldBe(serviceBase.Id);
        passwordSecret.ParentType.ShouldBe(EnvironmentVariableParentType.Service);
        passwordSecret.Value.ShouldNotBeNull();
        passwordSecret.Value.Value.ShouldBe("secret");
    }

    private static Service CreateServiceBase()
    {
        return Service.Create(Guid.NewGuid(), "test-service", ServiceType.DockerImage, ExposureMode.None);
    }
}