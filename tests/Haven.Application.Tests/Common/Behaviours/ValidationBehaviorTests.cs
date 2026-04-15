using FluentValidation;
using FluentValidation.Results;
using Haven.Application.Common;
using Haven.Application.Common.Behaviors;
using Mediator;
using NSubstitute;
using Shouldly;

namespace Haven.Application.Tests.Common.Behaviours;

[Category("Unit")]
public sealed class ValidationBehaviorTests
{
    private ValidationBehavior<TestMessage, Result> _sut;
    private IValidator<TestMessage> _validator;

    [SetUp]
    public void Setup()
    {
        _validator = Substitute.For<IValidator<TestMessage>>();
        _sut = new ValidationBehavior<TestMessage, Result>([_validator]);
    }

    [Test]
    public async Task Handle_ShouldCallNext_WhenNoValidationFailures()
    {
        var message = new TestMessage();

        _validator.Validate(message)
            .Returns(new ValidationResult());

        var expected = Result.Success();

        MessageHandlerDelegate<TestMessage, Result> next =
            (msg, ct) => ValueTask.FromResult(expected);

        var result = await _sut.Handle(message, next, CancellationToken.None);

        result.ShouldBe(expected);
    }

    [Test]
    public async Task Handle_ShouldReturnFailure_WhenValidationFails()
    {
        var message = new TestMessage();

        var failures = new[]
        {
            new ValidationFailure("Prop1", "Error 1"),
            new ValidationFailure("Prop2", "Error 2")
        };

        _validator.Validate(message)
            .Returns(new ValidationResult(failures));

        MessageHandlerDelegate<TestMessage, Result> next =
            (msg, ct) => ValueTask.FromResult(Result.Success());

        var result = await _sut.Handle(message, next, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.Message.ShouldContain("Error 1");
        result.Error.Message.ShouldContain("Error 2");
    }

    [Test]
    public async Task Handle_ShouldNotCallNext_WhenValidationFails()
    {
        var message = new TestMessage();

        var failures = new[]
        {
            new ValidationFailure("Prop", "Error")
        };

        _validator.Validate(message)
            .Returns(new ValidationResult(failures));

        var next = Substitute.For<MessageHandlerDelegate<TestMessage, Result>>();

        var result = await _sut.Handle(message, next, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();

        await next.DidNotReceive().Invoke(Arg.Any<TestMessage>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_ShouldReturnGenericFailure_WhenResponseIsGenericResult()
    {
        var validator = Substitute.For<IValidator<TestMessage>>();
        var sut = new ValidationBehavior<TestMessage, Result<string>>([validator]);

        var message = new TestMessage();

        var failures = new[]
        {
            new ValidationFailure("Prop", "Error")
        };

        validator.Validate(message)
            .Returns(new ValidationResult(failures));
        MessageHandlerDelegate<TestMessage, Result<string>> next =
            (msg, ct) => ValueTask.FromResult(Result<string>.Success("OK"));

        var result = await sut.Handle(message, next, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.Message.ShouldContain("Error");
    }

    [Test]
    public void Handle_ShouldThrow_WhenResponseTypeIsUnsupported()
    {
        var validator = Substitute.For<IValidator<TestMessage>>();
        var sut = new ValidationBehavior<TestMessage, object>([validator]);

        var message = new TestMessage();

        var failures = new[]
        {
            new ValidationFailure("Prop", "Error")
        };

        validator.Validate(message)
            .Returns(new ValidationResult(failures));

        MessageHandlerDelegate<TestMessage, object> next =
            (msg, ct) => ValueTask.FromResult(new object());

        var act = async () => await sut.Handle(message, next, CancellationToken.None);

        act.ShouldThrowAsync<InvalidOperationException>();
    }

    public sealed class TestMessage : IMessage;
}