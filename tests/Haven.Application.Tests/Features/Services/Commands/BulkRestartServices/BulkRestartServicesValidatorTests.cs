using FluentValidation.TestHelper;

using Haven.Application.Features.Services.Commands.BulkRestartServices;

namespace Haven.Application.Tests.Features.Services.Commands.BulkRestartServices;

[Category("Unit")]
public sealed class BulkRestartServicesValidatorTests
{
    private BulkRestartServicesValidator _sut;

    [SetUp]
    public void Setup() => _sut = new BulkRestartServicesValidator();

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
    public void Validate_ShouldHaveError_WhenServiceIdsIsEmpty()
    {
        var command = CreateCommand();
        command.ServiceIds = [];

        var result = _sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.ServiceIds);
    }

    [Test]
    public void Validate_ShouldHaveError_WhenServiceIdsContainsEmptyGuid()
    {
        var command = CreateCommand();
        command.ServiceIds = [Guid.Empty];

        var result = _sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor("ServiceIds[0]");
    }

    [Test]
    public void Validate_ShouldNotHaveError_WhenCommandIsValid()
    {
        var command = CreateCommand();

        var result = _sut.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    private static BulkRestartServicesCommand CreateCommand() => new()
    {
        ProjectId = Guid.NewGuid(),
        EnvironmentId = Guid.NewGuid(),
        ServiceIds = [Guid.NewGuid()],
    };
}
