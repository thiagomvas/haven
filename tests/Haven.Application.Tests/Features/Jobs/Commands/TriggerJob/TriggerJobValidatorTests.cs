using FluentValidation.TestHelper;

using Haven.Application.Features.Jobs.Commands.TriggerJob;

namespace Haven.Application.Tests.Features.Jobs.Commands.TriggerJob;

[Category("Unit")]
public sealed class TriggerJobValidatorTests
{
    private TriggerJobValidator _sut;

    [SetUp]
    public void Setup()
    {
        _sut = new TriggerJobValidator();
    }

    [Test]
    public void Validate_ShouldHaveError_WhenJobKeyIsEmpty()
    {
        var command = new TriggerJobCommand(string.Empty);

        var result = _sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.JobKey);
    }

    [Test]
    public void Validate_ShouldHaveError_WhenJobKeyExceedsMaximumLength()
    {
        var command = new TriggerJobCommand(new string('a', 101));

        var result = _sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.JobKey);
    }

    [Test]
    public void Validate_ShouldNotHaveError_WhenJobKeyIsValid()
    {
        var command = new TriggerJobCommand("some-job-key");

        var result = _sut.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(x => x.JobKey);
    }
}
