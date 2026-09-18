using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Configuration;
using Haven.Application.Features.Exporting;
using Haven.Application.Features.Services.Commands.ExportServiceToDockerCompose;
using Haven.Domain.Aggregates;
using Haven.Domain.Enums;
using Haven.Domain.ValueObjects;

using Microsoft.Extensions.Options;

using NSubstitute;

using Shouldly;

using DomainEnvironmentVariables = Haven.Domain.Entities.EnvironmentVariables;

namespace Haven.Application.Tests.Features.Services.Commands.ExportServiceToDockerCompose;

[Category("Unit")]
public sealed class ExportServiceToDockerComposeHandlerTests
{
    private IServiceRepository _serviceRepository = null!;
    private IEnvironmentVariableService _environmentVariableService = null!;
    private IFeatureFlagService _featureFlagService = null!;
    private IExportFormatFactory _exportFormatFactory = null!;
    private IExportFormat _exportFormat = null!;
    private IOptionsMonitor<VolumesOptions> _volumesOptions = null!;
    private ExportServiceToDockerComposeHandler _sut = null!;

    [SetUp]
    public void Setup()
    {
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

        _sut = new ExportServiceToDockerComposeHandler(
            _serviceRepository,
            _environmentVariableService,
            _featureFlagService,
            _exportFormatFactory,
            _volumesOptions);
    }

    private static Service NewDockerImageService(string image = "nginx:latest") =>
        Service.Create(Guid.NewGuid(), "test-service", ServiceType.DockerImage, ExposureMode.None,
            sourceConfig: new DockerConfig { Image = image });

    private static Service NewRawDockerfileService() =>
        Service.Create(Guid.NewGuid(), "test-service", ServiceType.Dockerfile, ExposureMode.None,
            sourceConfig: new DockerfileConfig { Source = DockerfileSource.Raw, Content = "FROM scratch" });

    private static Service NewGitDockerfileService(string filePath = "docker/Dockerfile") =>
        Service.Create(Guid.NewGuid(), "test-service", ServiceType.Dockerfile, ExposureMode.None,
            sourceConfig: new DockerfileConfig
            {
                Source = DockerfileSource.Git,
                Repository = "org/repo",
                Branch = "main",
                FilePath = filePath
            });

    [Test]
    public async Task Handle_ServiceDoesNotExist_ReturnsNotFound()
    {
        var command = new ExportServiceToDockerComposeCommand { ServiceId = Guid.NewGuid() };
        _serviceRepository.GetByIdAsync(command.ServiceId, Arg.Any<CancellationToken>()).Returns((Service?)null);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("NOT_FOUND");
    }

    [Test]
    public async Task Handle_DockerImageService_ReturnsExportedComposeFile()
    {
        var service = NewDockerImageService();
        var command = new ExportServiceToDockerComposeCommand { ServiceId = service.Id };
        _serviceRepository.GetByIdAsync(command.ServiceId, Arg.Any<CancellationToken>()).Returns(service);
        _exportFormat.ExportService(Arg.Any<ServiceExportModel>()).Returns("services:\n  test-service:\n    image: nginx:latest\n");

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldContain("nginx:latest");
        _exportFormat.Received(1).ExportService(Arg.Is<ServiceExportModel>(m => m.Image == "nginx:latest" && m.DockerfilePath == null));
    }

    [Test]
    public async Task Handle_GitDockerfileService_ReturnsExportedComposeFile()
    {
        var service = NewGitDockerfileService();
        var command = new ExportServiceToDockerComposeCommand { ServiceId = service.Id };
        _serviceRepository.GetByIdAsync(command.ServiceId, Arg.Any<CancellationToken>()).Returns(service);
        _exportFormat.ExportService(Arg.Any<ServiceExportModel>()).Returns("services:\n  test-service:\n    build:\n      dockerfile: docker/Dockerfile\n");

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        _exportFormat.Received(1).ExportService(Arg.Is<ServiceExportModel>(m => m.Image == null && m.DockerfilePath == "docker/Dockerfile"));
    }

    [Test]
    public async Task Handle_RawDockerfileService_ReturnsNotSupported_AndDoesNotCallExportFormat()
    {
        var service = NewRawDockerfileService();
        var command = new ExportServiceToDockerComposeCommand { ServiceId = service.Id };
        _serviceRepository.GetByIdAsync(command.ServiceId, Arg.Any<CancellationToken>()).Returns(service);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("NOT_SUPPORTED");
        _exportFormat.DidNotReceiveWithAnyArgs().ExportService(default!);
    }

    [Test]
    public async Task Handle_ShouldMergeFeatureFlagsIntoEnvironmentVariables()
    {
        var service = NewDockerImageService();
        var command = new ExportServiceToDockerComposeCommand { ServiceId = service.Id };
        _serviceRepository.GetByIdAsync(command.ServiceId, Arg.Any<CancellationToken>()).Returns(service);
        _environmentVariableService.BuildVariablesForServiceAsync(service.Id, Arg.Any<CancellationToken>())
            .Returns([new DomainEnvironmentVariables { Key = "NODE_ENV", Value = "production" }]);
        _featureFlagService.GetFlagsAsEnvironmentsForServiceAsync(service.Id, Arg.Any<CancellationToken>())
            .Returns(new List<DomainEnvironmentVariables> { new() { Key = "FEATURE_X", Value = "true" } });
        _exportFormat.ExportService(Arg.Any<ServiceExportModel>()).Returns("");

        await _sut.Handle(command, CancellationToken.None);

        _exportFormat.Received(1).ExportService(Arg.Is<ServiceExportModel>(m =>
            m.EnvironmentVariables["NODE_ENV"] == "production" &&
            m.EnvironmentVariables["FEATURE_X"] == "true"));
    }
}