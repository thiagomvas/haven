using Haven.Application.Features.Services;
using Haven.Application.Features.ServiceTemplates.Contracts;
using Haven.Application.Features.ServiceTemplates.Services;
using Haven.Domain.Aggregates;
using Haven.Domain.Entities;
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
        volumes[0].Source.ShouldContain("haven");
        volumes[0].Source.ShouldContain("data");
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

    [Test]
    public void AddComputedProperties_ResolvesInputsAndContainerPort_LeavesEnvAndRuntimeIntact()
    {
        var serviceBase = CreateServiceBase();
        var template = new ServiceTemplate
        {
            Container = new ServiceTemplateContainer { DockerImage = "postgres", Port = 5432 },
            Outputs = new List<ServiceTemplateOutput>
            {
                new()
                {
                    Key = "connection_string",
                    Label = "Connection String",
                    Secret = true,
                    Value = "postgresql://${{ env.POSTGRES_USER }}:${{ env.POSTGRES_PASSWORD }}@${{ runtime.host }}:${{ container.port }}/${{ inputs.database }}"
                }
            }
        };
        var inputValues = new Dictionary<string, string> { { "database", "app" } };

        var result = _sut.AddComputedProperties(serviceBase, template, inputValues);

        result.IsSuccess.ShouldBeTrue();
        var property = serviceBase.ComputedProperties.Single();
        property.Key.ShouldBe("connection_string");
        property.IsSecret.ShouldBeTrue();
        property.Template.ShouldBe("postgresql://${{ env.POSTGRES_USER }}:${{ env.POSTGRES_PASSWORD }}@${{ runtime.host }}:5432/app");
    }

    [Test]
    public void AddComputedProperties_UrlencodeFilter_IsPreservedForLaterResolution()
    {
        var serviceBase = CreateServiceBase();
        var template = new ServiceTemplate
        {
            Container = new ServiceTemplateContainer { DockerImage = "redis", Port = 6379 },
            Outputs = new List<ServiceTemplateOutput>
            {
                new() { Key = "connection_string", Label = "Connection String", Secret = true, Value = "redis://:${{ env.REDIS_PASSWORD | urlencode }}@${{ runtime.host }}:${{ container.port }}" }
            }
        };

        var result = _sut.AddComputedProperties(serviceBase, template, new Dictionary<string, string>());

        result.IsSuccess.ShouldBeTrue();
        serviceBase.ComputedProperties.Single().Template.ShouldBe("redis://:${{ env.REDIS_PASSWORD | urlencode }}@${{ runtime.host }}:6379");
    }

    [Test]
    public void AddComputedProperties_MissingPortWhenReferenced_ReturnsError()
    {
        var serviceBase = CreateServiceBase();
        var template = new ServiceTemplate
        {
            Container = new ServiceTemplateContainer { DockerImage = "redis" },
            Outputs = new List<ServiceTemplateOutput>
            {
                new() { Key = "connection_string", Label = "Connection String", Value = "redis://${{ runtime.host }}:${{ container.port }}" }
            }
        };

        var result = _sut.AddComputedProperties(serviceBase, template, new Dictionary<string, string>());

        result.IsFailure.ShouldBeTrue();
        serviceBase.ComputedProperties.ShouldBeEmpty();
    }

    [Test]
    public void AddComputedProperties_ReferencingSecretInputDirectly_ReturnsError()
    {
        var serviceBase = CreateServiceBase();
        var template = new ServiceTemplate
        {
            Inputs = new List<TemplateInputField> { new() { Key = "password", Type = TemplateInputFieldType.Secret } },
            Container = new ServiceTemplateContainer { DockerImage = "redis", Port = 6379 },
            Outputs = new List<ServiceTemplateOutput>
            {
                new() { Key = "connection_string", Label = "Connection String", Value = "redis://:${{ inputs.password }}@${{ runtime.host }}:${{ container.port }}" }
            }
        };

        var result = _sut.AddComputedProperties(serviceBase, template, new Dictionary<string, string> { { "password", "secret" } });

        result.IsFailure.ShouldBeTrue();
        serviceBase.ComputedProperties.ShouldBeEmpty();
    }

    [Test]
    public void AddComputedProperties_NoOutputs_IsNoOp()
    {
        var serviceBase = CreateServiceBase();
        var template = new ServiceTemplate { Container = new ServiceTemplateContainer { DockerImage = "redis" } };

        var result = _sut.AddComputedProperties(serviceBase, template, new Dictionary<string, string>());

        result.IsSuccess.ShouldBeTrue();
        serviceBase.ComputedProperties.ShouldBeEmpty();
    }

    [Test]
    public void AddCustomActions_ExecAction_ResolvesTemplateInputsAndKeepsActionInputs()
    {
        var service = CreateServiceBase();
        var template = TemplateWithAction(new ServiceTemplateAction
        {
            Name = "Dump",
            Alias = "dump",
            Description = "Dumps ${{ inputs.db }}",
            Icon = "database",
            Config = new ActionConfigManifest
            {
                Type = "exec",
                Command = ["pg_dump", "-U", "${{ inputs.user }}", "${{ inputs.db }}", "${{ inputs.table }}"],
                WorkingDir = "/data/${{ inputs.db }}",
                Shell = ShellType.Sh
            },
            RequiredPermissions = ["admin"],
            Risk = ActionRisk.RequireConfirmation,
            TimeoutSeconds = 120,
            Inputs = [new CustomActionInputManifest { Name = "table", Label = "Table", Required = true, DefaultValue = "${{ inputs.db }}_t" }]
        });

        var result = _sut.AddCustomActions(service, template, new Dictionary<string, string> { ["user"] = "pg", ["db"] = "app" });

        result.IsSuccess.ShouldBeTrue();
        var action = service.CustomActions.ShouldHaveSingleItem();
        action.ServiceId.ShouldBe(service.Id);
        action.ActionName.ShouldBe("Dump");
        action.Alias.ShouldBe("dump");
        action.Icon.ShouldBe("database");
        action.RequiredPermissions.ShouldBe(["admin"]);
        action.Risk.ShouldBe(ActionRisk.RequireConfirmation);
        action.Timeout.ShouldBe(TimeSpan.FromSeconds(120));
        action.Token.ShouldStartWith("hca_");
        var config = action.Config.ShouldBeOfType<ExecActionConfig>();
        config.Command.ShouldBe(["pg_dump", "-U", "pg", "app", "${{ inputs.table }}"]);
        config.WorkingDir.ShouldBe("/data/app");
        config.Shell.ShouldBe(ShellType.Sh);
        var input = action.Inputs.ShouldHaveSingleItem();
        input.Name.ShouldBe("table");
        input.Required.ShouldBeTrue();
        input.DefaultValue.ShouldBe("app_t");
    }

    [Test]
    public void AddCustomActions_HttpAction_ResolvesUrlHeadersAndBody()
    {
        var service = CreateServiceBase();
        var template = TemplateWithAction(new ServiceTemplateAction
        {
            Name = "Reload",
            Alias = "reload",
            Config = new ActionConfigManifest
            {
                Type = "http",
                Method = "POST",
                Url = "http://localhost:${{ inputs.port }}/reload",
                Headers = new Dictionary<string, string> { ["X-Tenant"] = "${{ inputs.tenant }}" },
                Body = "{\"t\":\"${{ inputs.tenant }}\"}",
                SuccessStatusCodes = [200]
            }
        });

        var result = _sut.AddCustomActions(service, template, new Dictionary<string, string> { ["port"] = "9090", ["tenant"] = "acme" });

        result.IsSuccess.ShouldBeTrue();
        var config = service.CustomActions.Single().Config.ShouldBeOfType<HttpActionConfig>();
        config.Method.ShouldBe(HttpMethod.Post);
        config.Url.ShouldBe("http://localhost:9090/reload");
        config.Headers["X-Tenant"].ShouldBe("acme");
        config.Body.ShouldBe("{\"t\":\"acme\"}");
        config.SuccessStatusCodes.ShouldBe([200]);
    }

    [Test]
    public void AddCustomActions_ReferencingSecretInput_Fails()
    {
        var service = CreateServiceBase();
        var template = TemplateWithAction(new ServiceTemplateAction
        {
            Name = "Login",
            Alias = "login",
            Config = new ActionConfigManifest { Type = "exec", Command = ["login", "${{ inputs.password }}"] }
        });
        template.Inputs.Add(new TemplateInputField { Key = "password", Type = TemplateInputFieldType.Secret, Label = "Password" });

        var result = _sut.AddCustomActions(service, template, new Dictionary<string, string> { ["password"] = "hunter2" });

        result.IsFailure.ShouldBeTrue();
        service.CustomActions.ShouldBeEmpty();
    }

    [Test]
    public void AddCustomActions_InvalidConfig_Fails()
    {
        var service = CreateServiceBase();
        var template = TemplateWithAction(new ServiceTemplateAction
        {
            Name = "Bad",
            Alias = "bad",
            Config = new ActionConfigManifest { Type = "http", Method = "GET", Url = "not-a-url" }
        });

        var result = _sut.AddCustomActions(service, template, new Dictionary<string, string>());

        result.IsFailure.ShouldBeTrue();
        service.CustomActions.ShouldBeEmpty();
    }

    [Test]
    public void AddCustomActions_DuplicateNames_Fails()
    {
        var service = CreateServiceBase();
        var action = new ServiceTemplateAction
        {
            Name = "Run",
            Alias = "run",
            Config = new ActionConfigManifest { Type = "exec", Command = ["true"] }
        };
        var template = TemplateWithAction(action);
        template.Actions.Add(action);

        _sut.AddCustomActions(service, template, new Dictionary<string, string>()).IsFailure.ShouldBeTrue();
    }

    [Test]
    public void AddCustomActions_NoActions_IsNoOp()
    {
        var service = CreateServiceBase();

        var result = _sut.AddCustomActions(service, new ServiceTemplate(), new Dictionary<string, string>());

        result.IsSuccess.ShouldBeTrue();
        service.CustomActions.ShouldBeEmpty();
    }

    private static ServiceTemplate TemplateWithAction(ServiceTemplateAction action) => new()
    {
        Container = new ServiceTemplateContainer { DockerImage = "img" },
        Inputs =
        [
            new TemplateInputField { Key = "user", Type = TemplateInputFieldType.Text, Label = "User" },
            new TemplateInputField { Key = "db", Type = TemplateInputFieldType.Text, Label = "Db" },
            new TemplateInputField { Key = "port", Type = TemplateInputFieldType.Text, Label = "Port" },
            new TemplateInputField { Key = "tenant", Type = TemplateInputFieldType.Text, Label = "Tenant" }
        ],
        Actions = [action]
    };

    private static Service CreateServiceBase()
    {
        return Service.Create(Guid.NewGuid(), "test-service", ServiceType.DockerImage, ExposureMode.None);
    }
}