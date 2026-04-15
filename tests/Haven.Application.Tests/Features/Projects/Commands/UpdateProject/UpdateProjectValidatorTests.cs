using FluentValidation.TestHelper;
using Haven.Application.Features.Projects.Commands.UpdateProject;
using Haven.Domain;

namespace Haven.Application.Tests.Features.Projects.Commands.UpdateProject;

[Category("Unit")]
public sealed class UpdateProjectValidatorTests
{
    private UpdateProjectValidator _sut;

    [SetUp]
    public void Setup()
    {
        _sut = new UpdateProjectValidator();
    }

    [Test]
    public void Validate_ShouldHaveError_WhenNameIsEmpty()
    {
        var command = CreateCommand();
        command.Name = "";

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

    [Test]
    public void Validate_ShouldNotHaveError_WhenNameIsNotProvided()
    {
        var command = CreateCommand();
        command.Name = Optional<string>.None;

        var result = _sut.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(x => x.Name);
    }

    [Test]
    public void Validate_ShouldNotHaveError_WhenNameIsValid()
    {
        var command = CreateCommand();
        command.Name = "Valid Name";

        var result = _sut.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(x => x.Name);
    }

    [Test]
    public void Validate_ShouldHaveError_WhenDescriptionExceedsMaxLength()
    {
        var command = CreateCommand();
        command.Description = new string('a', 501);

        var result = _sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Description);
    }

    [Test]
    public void Validate_ShouldNotHaveError_WhenDescriptionIsNull()
    {
        var command = CreateCommand();
        command.Description = null;

        var result = _sut.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(x => x.Description);
    }

    [Test]
    public void Validate_ShouldNotHaveError_WhenDescriptionIsNotProvided()
    {
        var command = CreateCommand();
        command.Description = Optional<string?>.None;

        var result = _sut.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(x => x.Description);
    }

    [Test]
    public void Validate_ShouldNotHaveError_WhenDescriptionIsWithinLimit()
    {
        var command = CreateCommand();
        command.Description = new string('a', 50);

        var result = _sut.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(x => x.Description);
    }

    private static UpdateProjectCommand CreateCommand()
    {
        return new UpdateProjectCommand
        {
            Name = Optional<string>.None,
            Description = Optional<string?>.None
        };
    }
}