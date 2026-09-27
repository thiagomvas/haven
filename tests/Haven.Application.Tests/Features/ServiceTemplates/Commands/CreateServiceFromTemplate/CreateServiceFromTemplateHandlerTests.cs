using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Features.ServiceTemplates.Commands.CreateServiceFromTemplate;
using Haven.Application.Features.ServiceTemplates.Contracts;
using Haven.Application.Features.ServiceTemplates.Services;
using Haven.Domain.Aggregates;
using Haven.Domain.Entities;
using Haven.Domain.Enums;
using Haven.Domain.ValueObjects;

using NSubstitute;

using Shouldly;

namespace Haven.Application.Tests.Features.ServiceTemplates.Commands.CreateServiceFromTemplate;

[Category("Unit")]
public sealed class CreateServiceFromTemplateHandlerTests
{
    private IProjectRepository _projectRepository;
    private IServiceRepository _serviceRepository;
    private IServiceTemplateRepository _templateRepository;
    private IEnvironmentVariableRepository _environmentVariableRepository;
    private ISecretVariableRepository _secretVariableRepository;
    private CreateServiceFromTemplateHandler _sut;

    [SetUp]
    public void Setup()
    {
        _projectRepository = Substitute.For<IProjectRepository>();
        _serviceRepository = Substitute.For<IServiceRepository>();
        _templateRepository = Substitute.For<IServiceTemplateRepository>();
        _environmentVariableRepository = Substitute.For<IEnvironmentVariableRepository>();
        _secretVariableRepository = Substitute.For<ISecretVariableRepository>();
        _sut = new CreateServiceFromTemplateHandler(
            _projectRepository,
            _serviceRepository,
            _templateRepository,
            _environmentVariableRepository,
            _secretVariableRepository,
            new ServiceTemplateInstantiator());
    }

    [Test]
    public async Task Handle_ShouldReturnFailure_WhenTemplateDoesNotExist()
    {
        var command = CreateCommand();
        _templateRepository.GetByIdAsync(command.TemplateId, Arg.Any<CancellationToken>())
            .Returns((ServiceTemplate?)null);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
    }

    [Test]
    public async Task Handle_ShouldReturnFailure_WhenTemplateHasNoContainerImage()
    {
        var command = CreateCommand();
        var template = CreateTemplate();
        template.Container = new ServiceTemplateContainer();
        _templateRepository.GetByIdAsync(command.TemplateId, Arg.Any<CancellationToken>())
            .Returns(template);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
    }

    [Test]
    public async Task Handle_ShouldReturnFailure_WhenProjectDoesNotExist()
    {
        var command = CreateCommand();
        _templateRepository.GetByIdAsync(command.TemplateId, Arg.Any<CancellationToken>())
            .Returns(CreateTemplate());
        _projectRepository.GetByIdAsync(command.ProjectId, Arg.Any<CancellationToken>())
            .Returns((Project?)null);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
    }

    [Test]
    public async Task Handle_ShouldReturnFailure_WhenEnvironmentDoesNotExist()
    {
        var command = CreateCommand();
        _templateRepository.GetByIdAsync(command.TemplateId, Arg.Any<CancellationToken>())
            .Returns(CreateTemplate());
        var project = Project.Create("test-project");
        _projectRepository.GetByIdAsync(command.ProjectId, Arg.Any<CancellationToken>())
            .Returns(project);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
    }

    [Test]
    public async Task Handle_ShouldReturnFailure_WhenServiceNameAlreadyExists()
    {
        var command = CreateCommand();
        var template = CreateTemplate();
        _templateRepository.GetByIdAsync(command.TemplateId, Arg.Any<CancellationToken>())
            .Returns(template);
        var project = Project.Create("test-project");
        var environment = project.AddEnvironment("staging");
        command.EnvironmentId = environment.Id;
        project.AddService(environment.Id, template.Name, Domain.Enums.ServiceType.DockerImage, Domain.Enums.ExposureMode.None);
        _projectRepository.GetByIdAsync(command.ProjectId, Arg.Any<CancellationToken>())
            .Returns(project);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
    }

    [Test]
    public async Task Handle_ShouldReturnFailure_WhenRequiredInputIsMissing()
    {
        var command = CreateCommand();
        command.InputValues = new Dictionary<string, string>();
        var template = CreateTemplate();
        _templateRepository.GetByIdAsync(command.TemplateId, Arg.Any<CancellationToken>())
            .Returns(template);
        var project = Project.Create("test-project");
        var environment = project.AddEnvironment("staging");
        command.EnvironmentId = environment.Id;
        _projectRepository.GetByIdAsync(command.ProjectId, Arg.Any<CancellationToken>())
            .Returns(project);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
    }

    [Test]
    public async Task Handle_ShouldReturnFailure_WhenSelectInputIsNotAValidOption()
    {
        var command = CreateCommand();
        command.InputValues["version"] = "999";
        var template = CreateTemplate();
        _templateRepository.GetByIdAsync(command.TemplateId, Arg.Any<CancellationToken>())
            .Returns(template);
        var project = Project.Create("test-project");
        var environment = project.AddEnvironment("staging");
        command.EnvironmentId = environment.Id;
        _projectRepository.GetByIdAsync(command.ProjectId, Arg.Any<CancellationToken>())
            .Returns(project);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
    }

    [Test]
    public async Task Handle_ShouldCreateServiceAndPersistEnvVarsAndSecrets()
    {
        var command = CreateCommand();
        var template = CreateTemplate();
        _templateRepository.GetByIdAsync(command.TemplateId, Arg.Any<CancellationToken>())
            .Returns(template);
        var project = Project.Create("test-project");
        var environment = project.AddEnvironment("staging");
        command.EnvironmentId = environment.Id;
        _projectRepository.GetByIdAsync(command.ProjectId, Arg.Any<CancellationToken>())
            .Returns(project);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBe(Guid.Empty);

        var service = environment.Services.FirstOrDefault(s => s.Id == result.Value);
        service.ShouldNotBeNull();

        await _serviceRepository.Received(1).AddAsync(Arg.Any<Service>(), Arg.Any<CancellationToken>());
        await _environmentVariableRepository.Received(1)
            .AddAsync(Arg.Is<IEnumerable<Haven.Domain.Entities.EnvironmentVariables>>(v => v.Count() == 1), Arg.Any<CancellationToken>());
        await _secretVariableRepository.Received(1).AddAsync(Arg.Any<SecretVariable>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_ShouldApplyRequestedExposureModeAndPorts()
    {
        var command = CreateCommand();
        command.ExposureMode = ExposureMode.External;
        command.Ports = ["8080:80"];
        var template = CreateTemplate();
        _templateRepository.GetByIdAsync(command.TemplateId, Arg.Any<CancellationToken>())
            .Returns(template);
        var project = Project.Create("test-project");
        var environment = project.AddEnvironment("staging");
        command.EnvironmentId = environment.Id;
        _projectRepository.GetByIdAsync(command.ProjectId, Arg.Any<CancellationToken>())
            .Returns(project);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        var service = environment.Services.First(s => s.Id == result.Value);
        service.ExposureMode.ShouldBe(ExposureMode.External);
        service.SourceConfig.ShouldBeOfType<DockerConfig>();
        ((DockerConfig)service.SourceConfig!).Ports.ShouldBe(["8080:80"]);
    }

    [Test]
    public async Task Handle_TemplateWithOutputs_CreatesComputedPropertiesOnService()
    {
        var command = CreateCommand();
        var template = CreateTemplate();
        template.Container.Port = 5432;
        template.Outputs =
        [
            new ServiceTemplateOutput
            {
                Key = "connection_string",
                Label = "Connection String",
                Secret = true,
                Value = "postgresql://${{ env.POSTGRES_USER }}:${{ env.POSTGRES_PASSWORD }}@${{ runtime.host }}:${{ container.port }}/db"
            }
        ];
        _templateRepository.GetByIdAsync(command.TemplateId, Arg.Any<CancellationToken>())
            .Returns(template);
        var project = Project.Create("test-project");
        var environment = project.AddEnvironment("staging");
        command.EnvironmentId = environment.Id;
        _projectRepository.GetByIdAsync(command.ProjectId, Arg.Any<CancellationToken>())
            .Returns(project);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        var service = environment.Services.First(s => s.Id == result.Value);
        var property = service.ComputedProperties.Single();
        property.Key.ShouldBe("connection_string");
        property.IsSecret.ShouldBeTrue();
        property.Template.ShouldBe("postgresql://${{ env.POSTGRES_USER }}:${{ env.POSTGRES_PASSWORD }}@${{ runtime.host }}:5432/db");
        property.Template.ShouldNotContain("secret");
    }

    [Test]
    public async Task Handle_OutputReferencingUnknownPort_ReturnsFailureAndDoesNotPersist()
    {
        var command = CreateCommand();
        var template = CreateTemplate();
        template.Outputs =
        [
            new ServiceTemplateOutput { Key = "connection_string", Label = "Connection String", Value = "postgresql://${{ runtime.host }}:${{ container.port }}/db" }
        ];
        _templateRepository.GetByIdAsync(command.TemplateId, Arg.Any<CancellationToken>())
            .Returns(template);
        var project = Project.Create("test-project");
        var environment = project.AddEnvironment("staging");
        command.EnvironmentId = environment.Id;
        _projectRepository.GetByIdAsync(command.ProjectId, Arg.Any<CancellationToken>())
            .Returns(project);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
        await _serviceRepository.DidNotReceive().AddAsync(Arg.Any<Service>(), Arg.Any<CancellationToken>());
    }

    private static CreateServiceFromTemplateCommand CreateCommand() => new()
    {
        ProjectId = Guid.NewGuid(),
        EnvironmentId = Guid.NewGuid(),
        TemplateId = "postgres",
        InputValues = new Dictionary<string, string>
        {
            { "version", "17" },
            { "password", "secret" }
        }
    };

    private static ServiceTemplate CreateTemplate() => new()
    {
        Id = "postgres",
        Version = Domain.ValueObjects.Version.Create(1, 0, 0),
        Name = "PostgreSQL",
        Icon = "postgres.svg",
        Category = "database",
        Inputs =
        [
            new TemplateInputField { Key = "version", Type = TemplateInputFieldType.Select, Options = ["15", "16", "17"], DefaultValue = "17" },
            new TemplateInputField { Key = "password", Type = TemplateInputFieldType.Secret }
        ],
        Container = new ServiceTemplateContainer
        {
            DockerImage = "postgres:${{ inputs.version }}-alpine",
            Env = new Dictionary<string, string>
            {
                { "POSTGRES_VERSION", "${{ inputs.version }}" },
                { "POSTGRES_PASSWORD", "${{ inputs.password }}" }
            }
        }
    };
}
