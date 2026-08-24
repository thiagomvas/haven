using FluentValidation.TestHelper;

using Haven.Application.Features.ServiceRegistry.Commands.AddDomain;

using Shouldly;

namespace Haven.Application.Tests.Features.ServiceRegistry.Commands.AddDomain;

[Category("Unit")]
public sealed class AddDomainValidatorTests
{
    private AddDomainValidator _sut;

    [SetUp]
    public void Setup()
    {
        _sut = new AddDomainValidator();
    }

    private static AddDomainCommand CreateCommand() =>
        new() { ServiceId = Guid.NewGuid(), Hostname = "example.com", ContainerPort = 8080 };

    [Test]
    public void Validate_ShouldHaveError_WhenServiceIdIsEmpty()
    {
        var command = CreateCommand();
        command.ServiceId = Guid.Empty;

        var result = _sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.ServiceId.Value);
    }

    [Test]
    public void Validate_ShouldHaveError_WhenHostnameIsEmpty()
    {
        var command = CreateCommand();
        command.Hostname = string.Empty;

        var result = _sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Hostname);
    }

    [TestCase("not a hostname!!")]
    [TestCase("http://example.com")]
    public void Validate_ShouldHaveError_WhenHostnameIsInvalid(string hostname)
    {
        var command = CreateCommand();
        command.Hostname = hostname;

        var result = _sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Hostname);
    }

    [Test]
    public void Validate_ShouldNotHaveError_WhenHostnameIsValid()
    {
        var command = CreateCommand();

        var result = _sut.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(x => x.Hostname);
    }

    [TestCase(0)]
    [TestCase(-1)]
    [TestCase(65536)]
    public void Validate_ShouldHaveError_WhenContainerPortIsOutOfRange(int port)
    {
        var command = CreateCommand();
        command.ContainerPort = port;

        var result = _sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.ContainerPort);
    }

    [Test]
    public void Validate_ShouldNotHaveError_WhenContainerPortIsValid()
    {
        var command = CreateCommand();

        var result = _sut.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(x => x.ContainerPort);
    }

    [Test]
    public void Validate_ShouldHaveError_WhenNeitherServiceIdNorSidecarIdProvided()
    {
        var command = CreateCommand();
        command.ServiceId = default;

        var result = _sut.TestValidate(command);

        result.Errors.ShouldNotBeEmpty();
    }

    [Test]
    public void Validate_ShouldHaveError_WhenBothServiceIdAndSidecarIdProvided()
    {
        var command = CreateCommand();
        command.SidecarId = Guid.NewGuid();

        var result = _sut.TestValidate(command);

        result.Errors.ShouldNotBeEmpty();
    }

    [Test]
    public void Validate_ShouldNotHaveError_WhenOnlySidecarIdProvided()
    {
        var command = new AddDomainCommand
        {
            SidecarId = Guid.NewGuid(), Hostname = "example.com", ContainerPort = 8080
        };

        var result = _sut.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }
}