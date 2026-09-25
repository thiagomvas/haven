using FluentValidation.TestHelper;

using Haven.Application.Features.Secrets.Commands.CreateSecretVariable;
using Haven.Domain.Enums;

namespace Haven.Application.Tests.Features.Secrets.Commands.CreateSecretVariable;

[Category("Unit")]
public sealed class CreateSecretVariableValidatorTests
{
    private CreateSecretVariableValidator _sut = null!;

    [SetUp]
    public void Setup()
    {
        _sut = new CreateSecretVariableValidator();
    }

    [Test]
    public void Validate_ShouldHaveError_WhenParentIdIsEmpty()
    {
        var command = CreateCommand();
        command.ParentId = Guid.Empty;

        var result = _sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.ParentId);
    }

    [Test]
    public void Validate_ShouldHaveError_WhenParentTypeIsInvalid()
    {
        var command = CreateCommand();
        command.ParentType = (EnvironmentVariableParentType)999;

        var result = _sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.ParentType);
    }

    [Test]
    public void Validate_ShouldHaveError_WhenKeyIsEmpty()
    {
        var command = CreateCommand();
        command.Key = string.Empty;

        var result = _sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Key);
    }

    [Test]
    public void Validate_ShouldHaveError_WhenKeyExceedsMaxLength()
    {
        var command = CreateCommand();
        command.Key = new string('a', 129);

        var result = _sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Key);
    }

    [Test]
    public void Validate_ShouldNotHaveError_WhenKeyIsAtMaxLength()
    {
        var command = CreateCommand();
        command.Key = new string('a', 128);

        var result = _sut.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(x => x.Key);
    }

    [Test]
    public void Validate_ShouldHaveError_WhenValueIsEmpty()
    {
        var command = CreateCommand();
        command.Value = string.Empty;

        var result = _sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Value);
    }

    [Test]
    public void Validate_ShouldNotHaveError_WhenCommandIsValid()
    {
        var command = CreateCommand();

        var result = _sut.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    private static CreateSecretVariableCommand CreateCommand() => new()
    {
        ParentId = Guid.NewGuid(),
        ParentType = EnvironmentVariableParentType.Service,
        Key = "API_KEY",
        Value = "super-secret"
    };
}
