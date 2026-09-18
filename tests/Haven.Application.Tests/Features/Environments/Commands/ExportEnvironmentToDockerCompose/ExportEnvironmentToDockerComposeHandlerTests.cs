using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Configuration;
using Haven.Application.Features.Environments.Commands.ExportEnvironmentToDockerCompose;
using Haven.Application.Features.Exporting;
using Haven.Domain.Aggregates;
using Haven.Domain.Enums;
using Haven.Domain.ValueObjects;

using Microsoft.Extensions.Options;

using NSubstitute;

using Shouldly;

using DomainEnvironmentVariables = Haven.Domain.Entities.EnvironmentVariables;
using Environment = Haven.Domain.Aggregates.Environment;

namespace Haven.Application.Tests.Features.Environments.Commands.ExportEnvironmentToDockerCompose;

[Category("Unit")]
public sealed class ExportEnvironmentToDockerComposeHandlerTests
{
    private IEnvironmentRepository _environmentRepository = null!;
    private IServiceRepository _serviceRepository = null!;
    private IEnvironmentVariableService _environmentVariableService = null!;
    private IFeatureFlagService _featureFlagService = null!;
    private IExportFormatFactory _exportFormatFactory = null!;
    private IExportFormat _exportFormat = null!;
    private IOptionsMonitor<VolumesOptions> _volumesOptions = null!;
    private ExportEnvironmentToDockerComposeHandler _sut = null!;

    [SetUp]
    public void Setup()
    {
        _environmentRepository = Substitute.For<IEnvironmentRepository>();
        _serviceRepository = Substitute.For<IServiceRepository>();
        _environmentVariableService = Substitute.For<IEnvironmentVariableService>();
        _featureFlagService = Substitute.For<IFeatureFlagService>();
        _exportFormatFactory = Substitute.For<IExportFormatFactory>();
        _exportFormat = Substitute.For<IExportFormat>();
        _volumesOptions = Substitute.For<IOptionsMonitor<VolumesOptions>>();

        _volumesOptions.CurrentValue.Returns(new VolumesOptions { RootPath = "/data/volumes" });
        _exportFormatFactory.Create(ExportFormatType.DockerCompose).Returns(_exportFormat);
        _environmentVariableService.BuildVariablesForServiceAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns([]);
        _featureFlagService.GetFlagsAsEnvironmentsForServiceAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new List<DomainEnvironmentVariables>());

        _sut = new ExportEnvironmentToDockerComposeHandler(
            _environmentRepository,
            _serviceRepository,
            _environmentVariableService,
            _featureFlagService,
            _exportFormatFactory,
            _volumesOptions);
    }

    private static Environment NewEnvironment() => Environment.Create(Guid.NewGuid(), "production");

    private static Service NewDockerImageService(Guid environmentId, string name, string image = "nginx:latest") =>
        Service.Create(environmentId, name, ServiceType.DockerImage, ExposureMode.None,
            sourceConfig: new DockerConfig { Image = image });

    [Test]
    public async Task Handle_EnvironmentDoesNotExist_ReturnsNotFound()
    {
        var command = new ExportEnvironmentToDockerComposeCommand { EnvironmentId = Guid.NewGuid() };
        _environmentRepository.GetByIdAsync(command.EnvironmentId, Arg.Any<CancellationToken>()).Returns((Environment?)null);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("NOT_FOUND");
    }

    [Test]
    public async Task Handle_ShouldOnlyIncludeSelectedServiceIds()
    {
        var environment = NewEnvironment();
        var selected = NewDockerImageService(environment.Id, "included");
        var notSelected = NewDockerImageService(environment.Id, "excluded");
        var command = new ExportEnvironmentToDockerComposeCommand
        {
            EnvironmentId = environment.Id,
            ServiceIds = [selected.Id]
        };
        _environmentRepository.GetByIdAsync(environment.Id, Arg.Any<CancellationToken>()).Returns(environment);
        _serviceRepository.GetByEnvironmentIdAsync(environment.Id, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Service>) [selected, notSelected]);
        _exportFormat.ExportEnvironment(Arg.Any<IReadOnlyList<ServiceExportModel>>()).Returns("");

        await _sut.Handle(command, CancellationToken.None);

        _exportFormat.Received(1).ExportEnvironment(Arg.Is<IReadOnlyList<ServiceExportModel>>(models =>
            models.Count == 1 && models[0].Name == "included"));
    }

    [Test]
    public async Task Handle_NoServicesSelected_ExportsEmptyList()
    {
        var environment = NewEnvironment();
        var service = NewDockerImageService(environment.Id, "some-service");
        var command = new ExportEnvironmentToDockerComposeCommand { EnvironmentId = environment.Id, ServiceIds = [] };
        _environmentRepository.GetByIdAsync(environment.Id, Arg.Any<CancellationToken>()).Returns(environment);
        _serviceRepository.GetByEnvironmentIdAsync(environment.Id, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Service>) [service]);
        _exportFormat.ExportEnvironment(Arg.Any<IReadOnlyList<ServiceExportModel>>()).Returns("");

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        _exportFormat.Received(1).ExportEnvironment(Arg.Is<IReadOnlyList<ServiceExportModel>>(models => models.Count == 0));
    }

    [Test]
    public async Task Handle_ShouldReturnFormattedResultFromExporter()
    {
        var environment = NewEnvironment();
        var service = NewDockerImageService(environment.Id, "svc");
        var command = new ExportEnvironmentToDockerComposeCommand { EnvironmentId = environment.Id, ServiceIds = [service.Id] };
        _environmentRepository.GetByIdAsync(environment.Id, Arg.Any<CancellationToken>()).Returns(environment);
        _serviceRepository.GetByEnvironmentIdAsync(environment.Id, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Service>) [service]);
        _exportFormat.ExportEnvironment(Arg.Any<IReadOnlyList<ServiceExportModel>>()).Returns("services:\n  svc:\n    image: nginx:latest\n");

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldContain("nginx:latest");
    }
}
