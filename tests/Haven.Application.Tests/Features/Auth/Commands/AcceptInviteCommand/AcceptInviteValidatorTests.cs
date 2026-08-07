using AcceptInviteCommandType = Haven.Application.Features.Auth.Commands.AcceptInviteCommand.AcceptInviteCommand;

using Haven.Application.Features.Auth.Commands.AcceptInviteCommand;

using Shouldly;

namespace Haven.Application.Tests.Features.Auth.Commands.AcceptInviteCommand;

[Category("Unit")]
public sealed class AcceptInviteValidatorTests
{
    private AcceptInviteValidator _sut;

    [SetUp]
    public void Setup()
    {
        _sut = new AcceptInviteValidator();
    }

    private static AcceptInviteCommandType CreateCommand() => new()
    {
        Token = "raw-token",
        Name = "Invitee Name",
        Password = "password123",
        ConfirmPassword = "password123"
    };

    [Test]
    public void Validate_WithValidCommand_ShouldSucceed()
    {
        var result = _sut.Validate(CreateCommand());

        result.IsValid.ShouldBeTrue();
    }

    [Test]
    public void Validate_WithEmptyToken_ShouldFail()
    {
        var command = CreateCommand();
        command.Token = "";

        var result = _sut.Validate(command);

        result.IsValid.ShouldBeFalse();
    }

    [Test]
    public void Validate_WithEmptyName_ShouldFail()
    {
        var command = CreateCommand();
        command.Name = "";

        var result = _sut.Validate(command);

        result.IsValid.ShouldBeFalse();
    }

    [Test]
    public void Validate_WithShortPassword_ShouldFail()
    {
        var command = CreateCommand();
        command.Password = "short";
        command.ConfirmPassword = "short";

        var result = _sut.Validate(command);

        result.IsValid.ShouldBeFalse();
    }

    [Test]
    public void Validate_WithMismatchedConfirmPassword_ShouldFail()
    {
        var command = CreateCommand();
        command.ConfirmPassword = "different-password";

        var result = _sut.Validate(command);

        result.IsValid.ShouldBeFalse();
    }
}
