using FluentValidation.TestHelper;
using Haven.Application.Features.Projects.Commands.CreateProject;

namespace  Haven.Application.Tests.Features.Projects.Commands.CreateProject;

[Category("Unit")]
public sealed class CreateProjectValidatorTests
{
    private CreateProjectValidator _sut;

    [SetUp]
    public void Setup()
    {
        _sut = new CreateProjectValidator();
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

    [Test]
    public void Validate_ShouldNotHaveError_WhenNameIsValid()
    {
        var command = CreateCommand();
        command.Name = "Valid Project";

        var result = _sut.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(x => x.Name);
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
    public void Validate_ShouldHaveError_WhenDescriptionExceedsMaxLength()
    {
        var command = CreateCommand();
        command.Description = new string('a', 501);

        var result = _sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Description);
    }

    [Test]
    public void Validate_ShouldNotHaveError_WhenDescriptionIsWithinLimit()
    {
        var command = CreateCommand();
        command.Description = new string('a', 500);

        var result = _sut.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(x => x.Description);
    }

    private static CreateProjectCommand CreateCommand()
    {
        return new()
        {
            Name = "Project Name",
            Description = "Project Description"
        };
    }
}