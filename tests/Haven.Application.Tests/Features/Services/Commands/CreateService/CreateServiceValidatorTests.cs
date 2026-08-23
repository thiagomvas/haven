using FluentValidation.TestHelper;

using Haven.Application.Features.Services.Commands.CreateService;
using Haven.Domain;
using Haven.Domain.Enums;

namespace Haven.Application.Tests.Features.Services.Commands.CreateService;

[Category("Unit")]
public sealed class CreateServiceValidatorTests
{
    private CreateServiceValidator _sut;

    [SetUp]
    public void Setup() => _sut = new CreateServiceValidator();

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
    public void Validate_ShouldHaveError_WhenNameIsEmpty()
    {
        var command = CreateCommand();
        command.Name = string.Empty;

        var result = _sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Test]
    public void Validate_ShouldHaveError_WhenNameIsNull()
    {
        var command = CreateCommand();
        command.Name = null!;

        var result = _sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [TestCase("web!")]
    [TestCase("web@api")]
    [TestCase("web#service")]
    public void Validate_ShouldHaveError_WhenNameHasInvalidFormat(string invalidName)
    {
        var command = CreateCommand();
        command.Name = invalidName;

        var result = _sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [TestCase("haven")]
    [TestCase("dns")]
    [TestCase("localhost")]
    [TestCase("host")]
    [TestCase("internal")]
    public void Validate_ShouldHaveError_WhenNameIsReserved(string reservedName)
    {
        var command = CreateCommand();
        command.Name = reservedName;

        var result = _sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Name);
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
        command.Name = validName;

        var result = _sut.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(x => x.Name);
    }

    [Test]
    public void Validate_ShouldHaveError_WhenTypeIsInvalid()
    {
        var command = CreateCommand();
        command.Type = (ServiceType)99;

        var result = _sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Type);
    }

    [Test]
    public void Validate_ShouldHaveError_WhenExposureModeIsInvalid()
    {
        var command = CreateCommand();
        command.ExposureMode = (ExposureMode)99;

        var result = _sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.ExposureMode);
    }

    [Test]
    public void Validate_ShouldNotHaveError_WhenCommandIsValid()
    {
        var command = CreateCommand();

        var result = _sut.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public void Validate_ShouldNotHaveError_WhenExposureModeIsCustomAndPortsUseThreeSegmentFormat()
    {
        var command = CreateCommand();
        command.ExposureMode = ExposureMode.Custom;
        command.DockerConfig!.Ports = ["127.0.0.1:8080:80"];

        var result = _sut.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor("DockerConfig.Ports[0]");
    }

    [Test]
    public void Validate_ShouldNotHaveError_WhenExposureModeIsCustomAndPortsOmitHostIp()
    {
        var command = CreateCommand();
        command.ExposureMode = ExposureMode.Custom;
        command.DockerConfig!.Ports = ["8080:80"];

        var result = _sut.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor("DockerConfig.Ports[0]");
    }

    [Test]
    public void Validate_ShouldHaveError_WhenExposureModeIsCustomAndHostIpIsInvalid()
    {
        var command = CreateCommand();
        command.ExposureMode = ExposureMode.Custom;
        command.DockerConfig!.Ports = ["999.999.999.999:8080:80"];

        var result = _sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor("DockerConfig.Ports[0]");
    }

    [Test]
    public void Validate_ShouldHaveError_WhenExposureModeIsNotCustomAndThreeSegmentFormatIsUsed()
    {
        var command = CreateCommand();
        command.ExposureMode = ExposureMode.Internal;
        command.DockerConfig!.Ports = ["127.0.0.1:8080:80"];

        var result = _sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor("DockerConfig.Ports[0]");
    }

    [Test]
    public void Validate_ShouldHaveError_WhenCommandArgContainsEmptyString()
    {
        var command = CreateCommand();
        command.DockerConfig!.CommandArgs = ["--entrypoints.web.address=:80", "   "];

        var result = _sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor("DockerConfig.CommandArgs[1]");
    }

    [Test]
    public void Validate_ShouldNotHaveError_WhenCommandArgsAreValid()
    {
        var command = CreateCommand();
        command.DockerConfig!.CommandArgs = ["--entrypoints.web.address=:80", "--entrypoints.websecure.address=:443"];

        var result = _sut.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    private static CreateServiceCommand CreateCommand() => new()
    {
        ProjectId = Guid.NewGuid(),
        EnvironmentId = Guid.NewGuid(),
        Name = "web",
        Type = ServiceType.DockerImage,
        ExposureMode = ExposureMode.External,
        DockerConfig = new()
        {
            Image = "traefik/whoami"
        }
    };
}