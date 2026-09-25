using FluentValidation.TestHelper;

using Haven.Application.Features.Secrets.Commands.UpdateSecretVariable;

namespace Haven.Application.Tests.Features.Secrets.Commands.UpdateSecretVariable;

[Category("Unit")]
public sealed class UpdateSecretVariableValidatorTests
{
    private UpdateSecretVariableValidator _sut = null!;

    [SetUp]
    public void Setup()
    {
        _sut = new UpdateSecretVariableValidator();
    }

    [Test]
    public void Validate_ShouldNotHaveError_WhenKeyAndValueAreNotProvided()
    {
        var command = new UpdateSecretVariableCommand { Id = Guid.NewGuid() };

        var result = _sut.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public void Validate_ShouldHaveError_WhenKeyIsProvidedAndEmpty()
    {
        var command = new UpdateSecretVariableCommand { Id = Guid.NewGuid(), Key = string.Empty };

        var result = _sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Key.Value);
    }

    [Test]
    public void Validate_ShouldHaveError_WhenKeyExceedsMaxLength()
    {
        var command = new UpdateSecretVariableCommand { Id = Guid.NewGuid(), Key = new string('a', 129) };

        var result = _sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Key.Value);
    }

    [Test]
    public void Validate_ShouldNotHaveError_WhenKeyIsProvidedAndValid()
    {
        var command = new UpdateSecretVariableCommand { Id = Guid.NewGuid(), Key = "NEW_KEY" };

        var result = _sut.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(x => x.Key.Value);
    }

    [Test]
    public void Validate_ShouldHaveError_WhenValueIsProvidedAndEmpty()
    {
        var command = new UpdateSecretVariableCommand { Id = Guid.NewGuid(), Value = string.Empty };

        var result = _sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Value.Value);
    }
}