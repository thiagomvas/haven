using FluentValidation.TestHelper;

using Haven.Application.Features.ServiceTemplates.Commands.CreateServiceFromTemplate;
using Haven.Domain.Enums;

namespace Haven.Application.Tests.Features.ServiceTemplates.Commands.CreateServiceFromTemplate;

[Category("Unit")]
public sealed class CreateServiceFromTemplateValidatorTests
{
    private CreateServiceFromTemplateValidator _sut;

    [SetUp]
    public void Setup() => _sut = new CreateServiceFromTemplateValidator();

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
    public void Validate_ShouldHaveError_WhenTemplateIdIsEmpty()
    {
        var command = CreateCommand();
        command.TemplateId = string.Empty;

        var result = _sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.TemplateId);
    }

    [Test]
    public void Validate_ShouldNotHaveError_WhenNameIsNull()
    {
        var command = CreateCommand();
        command.Name = null;

        var result = _sut.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(x => x.Name);
    }

    [TestCase("web!")]
    [TestCase("web@api")]
    public void Validate_ShouldHaveError_WhenNameHasInvalidFormat(string invalidName)
    {
        var command = CreateCommand();
        command.Name = invalidName;

        var result = _sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [TestCase("haven")]
    [TestCase("dns")]
    public void Validate_ShouldHaveError_WhenNameIsReserved(string reservedName)
    {
        var command = CreateCommand();
        command.Name = reservedName;

        var result = _sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Test]
    public void Validate_ShouldNotHaveError_WhenAliasIsNull()
    {
        var command = CreateCommand();
        command.Alias = null;

        var result = _sut.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(x => x.Alias);
    }

    [TestCase("a")]
    [TestCase("-bad")]
    [TestCase("bad-")]
    public void Validate_ShouldHaveError_WhenAliasIsInvalid(string invalidAlias)
    {
        var command = CreateCommand();
        command.Alias = invalidAlias;

        var result = _sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Alias);
    }

    [Test]
    public void Validate_ShouldNotHaveError_WhenCommandIsValid()
    {
        var command = CreateCommand();

        var result = _sut.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public void Validate_ShouldHaveError_WhenExposureModeIsCustomAndHostIpIsInvalid()
    {
        var command = CreateCommand();
        command.ExposureMode = ExposureMode.Custom;
        command.Ports = ["999.999.999.999:8080:80"];

        var result = _sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor("Ports[0]");
    }

    [Test]
    public void Validate_ShouldNotHaveError_WhenExposureModeIsCustomAndPortsUseThreeSegmentFormat()
    {
        var command = CreateCommand();
        command.ExposureMode = ExposureMode.Custom;
        command.Ports = ["127.0.0.1:8080:80"];

        var result = _sut.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor("Ports[0]");
    }

    [Test]
    public void Validate_ShouldHaveError_WhenExposureModeIsNotCustomAndThreeSegmentFormatIsUsed()
    {
        var command = CreateCommand();
        command.ExposureMode = ExposureMode.Internal;
        command.Ports = ["127.0.0.1:8080:80"];

        var result = _sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor("Ports[0]");
    }

    private static CreateServiceFromTemplateCommand CreateCommand() => new()
    {
        ProjectId = Guid.NewGuid(),
        EnvironmentId = Guid.NewGuid(),
        TemplateId = "postgres",
        InputValues = new Dictionary<string, string> { { "postgres_password", "secret" } }
    };
}
