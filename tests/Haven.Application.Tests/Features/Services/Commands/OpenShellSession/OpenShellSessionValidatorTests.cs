using FluentValidation.TestHelper;

using Haven.Application.Features.Services.Commands.OpenShellSession;
using Haven.Domain.Enums;

namespace Haven.Application.Tests.Features.Services.Commands.OpenShellSession;

[Category("Unit")]
public sealed class OpenShellSessionValidatorTests
{
    private OpenShellSessionValidator _sut;

    [SetUp]
    public void Setup() => _sut = new OpenShellSessionValidator();

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
    public void Validate_ShouldHaveError_WhenShellTypeIsNotDefined()
    {
        var command = CreateCommand();
        command.ShellType = (ShellType)999;

        var result = _sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.ShellType);
    }

    [TestCase(ShellType.Bash)]
    [TestCase(ShellType.Sh)]
    public void Validate_ShouldNotHaveError_WhenCommandIsValid(ShellType shellType)
    {
        var command = CreateCommand();
        command.ShellType = shellType;

        var result = _sut.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    private static OpenShellSessionCommand CreateCommand() => new()
    {
        ProjectId = Guid.NewGuid(),
        EnvironmentId = Guid.NewGuid(),
        ServiceId = Guid.NewGuid(),
        ShellType = ShellType.Bash,
    };
}