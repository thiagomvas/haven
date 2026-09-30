using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Features.Services;
using Haven.Application.Features.Services.Commands.ImportFromManifest;
using Haven.Domain.Aggregates;
using Haven.Domain.Enums;

using NSubstitute;

using Shouldly;

using Environment = Haven.Domain.Aggregates.Environment;

namespace Haven.Application.Tests.Features.Services.Commands.ImportFromManifest;

[Category("Unit")]
public sealed class ImportFromManifestHandlerTests
{
    private const string Yaml = "name: web";

    private IManifestParser<ServiceManifestDto> _parser;
    private IEnvironmentRepository _environmentRepository;
    private IServiceRepository _serviceRepository;
    private ImportFromManifestHandler _sut;

    [SetUp]
    public void Setup()
    {
        _parser = Substitute.For<IManifestParser<ServiceManifestDto>>();
        _environmentRepository = Substitute.For<IEnvironmentRepository>();
        _serviceRepository = Substitute.For<IServiceRepository>();
        _sut = new ImportFromManifestHandler(_parser, _environmentRepository, _serviceRepository);
    }

    [Test]
    public async Task Handle_ShouldReturnNotFound_WhenEnvironmentDoesNotExist()
    {
        var command = CreateCommand();
        _environmentRepository.GetByIdAsync(command.EnvironmentId, Arg.Any<CancellationToken>())
            .Returns((Environment?)null);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
        result.Error.Code.ShouldBe("NOT_FOUND");
        await _serviceRepository.DidNotReceive().AddAsync(Arg.Any<Service>(), Arg.Any<CancellationToken>());
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public async Task Handle_ShouldReturnInvalidOperation_WhenRawManifestIsMissing(string? raw)
    {
        var command = CreateCommand(raw);
        SetupEnvironment(command);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
        result.Error.Code.ShouldBe("INVALID_OPERATION");
        await _parser.DidNotReceive().ParseAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _serviceRepository.DidNotReceive().AddAsync(Arg.Any<Service>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_ShouldAddServiceToRepository_AndReturnItsId()
    {
        var command = CreateCommand();
        var environment = SetupEnvironment(command);
        _parser.ParseAsync(Yaml, Arg.Any<CancellationToken>()).Returns(CreateManifest());

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        await _serviceRepository.Received(1).AddAsync(
            Arg.Is<Service>(s => s.Id == result.Value && s.EnvironmentId == environment.Id && s.Name == "web"),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_ShouldGenerateNewServiceId_IgnoringManifestId()
    {
        var command = CreateCommand();
        SetupEnvironment(command);
        var manifest = CreateManifest();
        var originalId = manifest.Id = Guid.NewGuid();
        _parser.ParseAsync(Yaml, Arg.Any<CancellationToken>()).Returns(manifest);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.Value.ShouldNotBe(originalId);
        result.Value.ShouldNotBe(Guid.Empty);
    }

    [Test]
    public async Task Handle_ShouldGenerateNewVolumeIds()
    {
        var command = CreateCommand();
        SetupEnvironment(command);
        var originalVolumeId = Guid.NewGuid();
        var manifest = CreateManifest();
        manifest.Volumes.Add(new VolumeManifest
        {
            Id = originalVolumeId,
            Type = VolumeType.Named,
            Name = "data",
            Target = "/data",
        });
        _parser.ParseAsync(Yaml, Arg.Any<CancellationToken>()).Returns(manifest);
        Service? saved = null;
        await _serviceRepository.AddAsync(Arg.Do<Service>(s => saved = s), Arg.Any<CancellationToken>());

        await _sut.Handle(command, CancellationToken.None);

        saved.ShouldNotBeNull();
        var volume = saved.Volumes.ShouldHaveSingleItem();
        volume.Id.ShouldNotBe(originalVolumeId);
        volume.Id.ShouldNotBe(Guid.Empty);
        volume.ServiceId.ShouldBe(saved.Id);
    }

    [Test]
    public async Task Handle_ShouldRegenerateToken()
    {
        var command = CreateCommand();
        SetupEnvironment(command);
        var manifest = CreateManifest();
        _parser.ParseAsync(Yaml, Arg.Any<CancellationToken>()).Returns(manifest);
        Service? saved = null;
        await _serviceRepository.AddAsync(Arg.Do<Service>(s => saved = s), Arg.Any<CancellationToken>());

        await _sut.Handle(command, CancellationToken.None);

        saved.ShouldNotBeNull();
        saved.Token.ShouldNotBe("original-token");
        saved.Token.ShouldNotBeNullOrWhiteSpace();
    }

    private Environment SetupEnvironment(ImportFromManifestCommand command)
    {
        var environment = Environment.Create(Guid.NewGuid(), "staging");
        command.EnvironmentId = environment.Id;
        _environmentRepository.GetByIdAsync(environment.Id, Arg.Any<CancellationToken>()).Returns(environment);
        return environment;
    }

    private static ImportFromManifestCommand CreateCommand(string? raw = Yaml) => new()
    {
        EnvironmentId = Guid.NewGuid(),
        RawManifest = raw,
    };

    private static ServiceManifestDto CreateManifest() => new()
    {
        Name = "web",
        Type = ServiceType.DockerImage,
        ExposureMode = ExposureMode.Internal,
        Status = ServiceStatus.Stopped,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
        Token = "original-token",
        SourceConfig = new ServiceSourceConfigManifest { Type = "docker", Image = "nginx" },
    };
}
