using FluentValidation.TestHelper;

using Haven.Application.Features.Environments.Commands.UpdateEnvironment;
using Haven.Domain;
using Haven.Domain.Entities;

using Environment = Haven.Domain.Aggregates.Environment;

namespace Haven.Application.Tests.Features.Environments.Commands.UpdateEnvironment;

[Category("Unit")]
public sealed class UpdateEnvironmentValidatorTests
{
    private UpdateEnvironmentValidator _sut;

    [SetUp]
    public void Setup()
    {
        _sut = new UpdateEnvironmentValidator();
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
    public void Validate_ShouldHaveError_WhenEnvironmentIdIsEmpty()
    {
        var command = CreateCommand();
        command.EnvironmentId = Guid.Empty;

        var result = _sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.EnvironmentId);
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
    public void Validate_ShouldHaveError_WhenNameIsProvidedButEmpty()
    {
        var command = CreateCommand();
        command.Name = string.Empty;

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
        command.Name = "production";

        var result = _sut.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(x => x.Name);
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

    private static UpdateEnvironmentCommand CreateCommand() => new()
    {
        ProjectId = Guid.NewGuid(),
        EnvironmentId = Guid.NewGuid(),
        Name = "staging",
        Description = "Updated description"
    };
}