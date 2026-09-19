using Haven.Application.Features.HealthChecks;
using Haven.Application.Features.HealthChecks.Commands.CreateHealthCheckCommand;
using Haven.Application.Features.HealthChecks.Commands.TestHealthCheckCommand;
using Haven.Application.Features.HealthChecks.Commands.UpdateHealthCheckCommand;
using Haven.Application.Features.HealthChecks.Queries.GetHealthCheckResultsQuery;
using Haven.Domain.Enums;

using Shouldly;

namespace Haven.Application.Tests.Features.HealthChecks;

[Category("Unit")]
public sealed class HealthCheckValidatorTests
{
    private static CreateHealthCheckCommand ValidCreate() => new()
    {
        ServiceId = Guid.NewGuid(),
        Name = "api",
        Kind = HealthCheckKind.Tcp,
        Config = """{"host":"{{container}}","port":8080}"""
    };

    [Test]
    public void Create_Valid_Passes() =>
        new CreateHealthCheckValidator().Validate(ValidCreate()).IsValid.ShouldBeTrue();

    [TestCase(-1)]
    [TestCase(11)]
    public void Create_RetriesOutOfRange_Fails(int retries)
    {
        var command = ValidCreate();
        command.Retries = retries;

        new CreateHealthCheckValidator().Validate(command).IsValid.ShouldBeFalse();
    }

    [TestCase(0)]
    [TestCase(101)]
    public void Create_ThresholdsOutOfRange_Fail(int threshold)
    {
        var failure = ValidCreate();
        failure.FailureThreshold = threshold;
        var success = ValidCreate();
        success.SuccessThreshold = threshold;

        new CreateHealthCheckValidator().Validate(failure).IsValid.ShouldBeFalse();
        new CreateHealthCheckValidator().Validate(success).IsValid.ShouldBeFalse();
    }

    [Test]
    public void Update_OmittedSettings_Pass()
    {
        var command = new UpdateHealthCheckCommand { HealthCheckId = Guid.NewGuid() };

        new UpdateHealthCheckValidator().Validate(command).IsValid.ShouldBeTrue();
    }

    [Test]
    public void Update_InvalidProvidedSettings_Fail()
    {
        var command = new UpdateHealthCheckCommand { HealthCheckId = Guid.NewGuid(), Retries = 50, FailureThreshold = 0 };

        var result = new UpdateHealthCheckValidator().Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.Count.ShouldBe(2);
    }

    [Test]
    public void Test_InvalidConfigForKind_Fails()
    {
        var command = new TestHealthCheckCommand { ServiceId = Guid.NewGuid(), Kind = HealthCheckKind.Http, Config = """{"url":""}""" };

        new TestHealthCheckValidator().Validate(command).IsValid.ShouldBeFalse();
    }

    [Test]
    public void Test_ValidConfig_Passes()
    {
        var command = new TestHealthCheckCommand { ServiceId = Guid.NewGuid(), Kind = HealthCheckKind.Container, Config = "" };

        new TestHealthCheckValidator().Validate(command).IsValid.ShouldBeTrue();
    }

    [TestCase(0, false)]
    [TestCase(1, true)]
    [TestCase(200, true)]
    [TestCase(201, false)]
    public void Results_LimitMustBeBounded(int limit, bool valid)
    {
        var query = new GetHealthCheckResultsQuery { HealthCheckId = Guid.NewGuid(), Limit = limit };

        new GetHealthCheckResultsValidator().Validate(query).IsValid.ShouldBe(valid);
    }
}
