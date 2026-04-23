using FluentValidation.TestHelper;
using Haven.Application.Features.Services.Commands.RestartService;

namespace Haven.Application.Tests.Features.Services.Commands.RestartService;

[Category("Unit")]
public sealed class RestartServiceValidatorTests
{
    private RestartServiceValidator _sut;

    [SetUp]
    public void Setup() => _sut = new RestartServiceValidator();

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
    public void Validate_ShouldNotHaveError_WhenCommandIsValid()
    {
        var command = CreateCommand();

        var result = _sut.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    private static RestartServiceCommand CreateCommand() => new()
    {
        ProjectId = Guid.NewGuid(),
        EnvironmentId = Guid.NewGuid(),
        ServiceId = Guid.NewGuid(),
    };
}
