using FluentValidation.TestHelper;

using Haven.Application.Features.Services.Commands.UpdateService;
using Haven.Domain;
using Haven.Domain.Enums;
using Haven.Domain.ValueObjects;

namespace Haven.Application.Tests.Features.Services.Commands.UpdateService;

[Category("Unit")]
public sealed class UpdateServiceValidatorTests
{
    private UpdateServiceValidator _sut;

    [SetUp]
    public void Setup() => _sut = new UpdateServiceValidator();

    [Test]
    public void Validate_ShouldHaveError_WhenProjectIdIsEmpty()
    {
        var command = CreateCommand();
        command.ProjectId = Guid.Empty;

        var result = _sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.ProjectId);
    }

    [Test]
    public void Validate_ShouldHaveError_WhenEnvironmentIdIsEmpty()
    {
        var command = CreateCommand();
        command.EnvironmentId = Guid.Empty;

        var result = _sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.EnvironmentId);
    }

    [Test]
    public void Validate_ShouldHaveError_WhenServiceIdIsEmpty()
    {
        var command = CreateCommand();
        command.ServiceId = Guid.Empty;

        var result = _sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.ServiceId);
    }

    [Test]
    public void Validate_ShouldHaveError_WhenNameIsEmpty()
    {
        var command = CreateCommand();
        command.Name = (Optional<string>)string.Empty;

        var result = _sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Test]
    public void Validate_ShouldHaveError_WhenNameIsWhitespace()
    {
        var command = CreateCommand();
        command.Name = (Optional<string>)"   ";

        var result = _sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [TestCase("web!")]
    [TestCase("web@api")]
    [TestCase("web#service")]
    public void Validate_ShouldHaveError_WhenNameHasInvalidFormat(string invalidName)
    {
        var command = CreateCommand();
        command.Name = (Optional<string>)invalidName;

        var result = _sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Name.Value);
    }

    [TestCase("haven")]
    [TestCase("dns")]
    [TestCase("localhost")]
    [TestCase("host")]
    [TestCase("internal")]
    public void Validate_ShouldHaveError_WhenNameIsReserved(string reservedName)
    {
        var command = CreateCommand();
        command.Name = (Optional<string>)reservedName;

        var result = _sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Name.Value);
    }

    [TestCase("web")]
    [TestCase("my-api")]
    [TestCase("worker-123")]
    [TestCase("WebService")]
    [TestCase("my service")]
    [TestCase("web_app")]
    [TestCase("My_Service-123")]
    public void Validate_ShouldNotHaveError_WhenNameIsValid(string validName)
    {
        var command = CreateCommand();
        command.Name = (Optional<string>)validName;

        var result = _sut.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(x => x.Name);
    }

    [Test]
    public void Validate_ShouldNotHaveError_WhenNameIsNotProvided()
    {
        var command = CreateCommand();
        command.Name = default;

        var result = _sut.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(x => x.Name);
    }

    [Test]
    public void Validate_ShouldHaveError_WhenTypeIsInvalid()
    {
        var command = CreateCommand();
        command.Type = (Optional<ServiceType>)(ServiceType)99;

        var result = _sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Type.Value);
    }

    [Test]
    public void Validate_ShouldNotHaveError_WhenTypeIsNotProvided()
    {
        var command = CreateCommand();
        command.Type = default;

        var result = _sut.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(x => x.Type);
    }

    [Test]
    public void Validate_ShouldHaveError_WhenExposureModeIsInvalid()
    {
        var command = CreateCommand();
        command.ExposureMode = (Optional<ExposureMode>)(ExposureMode)99;

        var result = _sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.ExposureMode.Value);
    }

    [Test]
    public void Validate_ShouldNotHaveError_WhenExposureModeIsNotProvided()
    {
        var command = CreateCommand();
        command.ExposureMode = default;

        var result = _sut.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(x => x.ExposureMode);
    }

    [Test]
    public void Validate_ShouldHaveError_WhenDockerImageTypeButNoDockerConfig()
    {
        var command = CreateCommand();
        command.Type = (Optional<ServiceType>)ServiceType.DockerImage;
        command.DockerConfig = (Optional<DockerConfig?>)(DockerConfig?)null;

        var result = _sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Type);
    }

    [Test]
    public void Validate_ShouldHaveError_WhenDockerImageButEmptyImage()
    {
        var command = CreateCommand();
        command.Type = (Optional<ServiceType>)ServiceType.DockerImage;
        command.DockerConfig = (Optional<DockerConfig?>)new DockerConfig { Image = string.Empty };

        var result = _sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.DockerConfig.Value.Image);
    }

    [Test]
    public void Validate_ShouldNotHaveError_WhenCommandIsMinimal()
    {
        var command = CreateCommand();

        var result = _sut.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public void Validate_ShouldNotHaveError_WhenAllFieldsProvided()
    {
        var command = CreateCommand();
        command.Name = "api";
        command.Type = ServiceType.DockerImage;
        command.ExposureMode = ExposureMode.Internal;
        command.DockerConfig = new DockerConfig { Image = "myapp:latest" };

        var result = _sut.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public void Validate_ShouldNotHaveError_WhenExposureModeIsCustomAndPortsUseThreeSegmentFormat()
    {
        var command = CreateCommand();
        command.ExposureMode = ExposureMode.Custom;
        command.DockerConfig = new DockerConfig { Image = "myapp:latest", Ports = ["127.0.0.1:8080:80"] };

        var result = _sut.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor("DockerConfig.Value.Ports[0]");
    }

    [Test]
    public void Validate_ShouldNotHaveError_WhenExposureModeIsCustomAndPortsOmitHostIp()
    {
        var command = CreateCommand();
        command.ExposureMode = ExposureMode.Custom;
        command.DockerConfig = new DockerConfig { Image = "myapp:latest", Ports = ["8080:80"] };

        var result = _sut.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor("DockerConfig.Value.Ports[0]");
    }

    [Test]
    public void Validate_ShouldHaveError_WhenExposureModeIsCustomAndHostIpIsInvalid()
    {
        var command = CreateCommand();
        command.ExposureMode = ExposureMode.Custom;
        command.DockerConfig = new DockerConfig { Image = "myapp:latest", Ports = ["999.999.999.999:8080:80"] };

        var result = _sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor("DockerConfig.Value.Ports[0]");
    }

    [Test]
    public void Validate_ShouldHaveError_WhenExposureModeIsNotCustomAndThreeSegmentFormatIsUsed()
    {
        var command = CreateCommand();
        command.ExposureMode = ExposureMode.Internal;
        command.DockerConfig = new DockerConfig { Image = "myapp:latest", Ports = ["127.0.0.1:8080:80"] };

        var result = _sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor("DockerConfig.Value.Ports[0]");
    }

    [Test]
    public void Validate_ShouldHaveError_WhenCommandArgContainsEmptyString()
    {
        var command = CreateCommand();
        command.DockerConfig = new DockerConfig { Image = "myapp:latest", CommandArgs = ["--foo=bar", " "] };

        var result = _sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor("DockerConfig.Value.CommandArgs[1]");
    }

    [Test]
    public void Validate_ShouldNotHaveError_WhenCommandArgsAreValid()
    {
        var command = CreateCommand();
        command.DockerConfig = new DockerConfig { Image = "myapp:latest", CommandArgs = ["--foo=bar"] };

        var result = _sut.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    private static UpdateServiceCommand CreateCommand() => new()
    {
        ProjectId = Guid.NewGuid(),
        EnvironmentId = Guid.NewGuid(),
        ServiceId = Guid.NewGuid()
    };
}