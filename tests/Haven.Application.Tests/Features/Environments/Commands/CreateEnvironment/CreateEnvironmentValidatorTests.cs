using FluentValidation.TestHelper;

using Haven.Application.Features.Environments.Commands.CreateEnvironment;
using Haven.Domain.Entities;

using Environment = Haven.Domain.Entities.Environment;

namespace Haven.Application.Tests.Features.Environments.Commands.CreateEnvironment;

[Category("Unit")]
public sealed class CreateEnvironmentValidatorTests
{
    private CreateEnvironmentValidator _sut;

    [SetUp]
    public void Setup()
    {
        _sut = new CreateEnvironmentValidator();
    }

    [Test]
    public void Validate_ShouldHaveError_WhenProjectIdIsEmpty()
    {
        var command = CreateCommand();
        command.ProjectId = Guid.Empty;

        var result = _sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.ProjectId);
    }

    [Test]
    public void Validate_ShouldNotHaveError_WhenProjectIdIsValid()
    {
        var command = CreateCommand();

        var result = _sut.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(x => x.ProjectId);
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
    public void Validate_ShouldHaveError_WhenNameExceedsMaxLength()
    {
        var command = CreateCommand();
        command.Name = new string('a', Environment.MaxNameLength + 1);

        var result = _sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Test]
    public void Validate_ShouldNotHaveError_WhenNameIsAtMaxLength()
    {
        var command = CreateCommand();
        command.Name = new string('a', Environment.MaxNameLength);

        var result = _sut.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(x => x.Name);
    }

    [TestCase("haven")]
    [TestCase("shared")]
    [TestCase("internal")]
    [TestCase("host")]
    [TestCase("HAVEN")]
    [TestCase("Shared")]
    public void Validate_ShouldHaveError_WhenNameIsReserved(string reservedName)
    {
        var command = CreateCommand();
        command.Name = reservedName;

        var result = _sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Test]
    public void Validate_ShouldNotHaveError_WhenNameIsValid()
    {
        var command = CreateCommand();
        command.Name = "staging";

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
        command.Description = new string('a', Environment.MaxDescriptionLength + 1);

        var result = _sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Description);
    }

    [Test]
    public void Validate_ShouldNotHaveError_WhenDescriptionIsWithinLimit()
    {
        var command = CreateCommand();
        command.Description = new string('a', Environment.MaxDescriptionLength);

        var result = _sut.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(x => x.Description);
    }

    private static CreateEnvironmentCommand CreateCommand() => new()
    {
        ProjectId = Guid.NewGuid(),
        Name = "staging",
        Description = "Staging environment"
    };
}