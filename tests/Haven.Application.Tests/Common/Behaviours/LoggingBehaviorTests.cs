using Haven.Application.Common;
using Haven.Application.Common.Behaviors;
using Mediator;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;

namespace Haven.Application.Tests.Common.Behaviours;

[Category("Unit")]
public sealed class LoggingBehaviorTests
{
    private LoggingBehavior<TestMessage, object> _sut;
    private ILogger<LoggingBehavior<TestMessage, object>> _logger;

    [SetUp]
    public void Setup()
    {
        _logger = Substitute.For<ILogger<LoggingBehavior<TestMessage, object>>>();
        _sut = new LoggingBehavior<TestMessage, object>(_logger);
    }

    [Test]
    public async Task Handle_ShouldLog_WhenResultIsFailure()
    {
        var message = new TestMessage();
        var failure = Result.Failure(Error.Conflict);
        MessageHandlerDelegate<TestMessage, object> next = (msg, ct) => ValueTask.FromResult((object)failure);

        var result = await _sut.Handle(message, next, CancellationToken.None);

        result.ShouldBe(failure);

        _logger.Received().Log(
            Arg.Any<LogLevel>(),
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Test]
    public async Task Handle_ShouldLogAndRethrow_WhenExceptionOccurs()
    {
        var message = new TestMessage();
        var exception = new InvalidOperationException("Boom");
        MessageHandlerDelegate<TestMessage, object> next =
            (msg, ct) => throw exception;
        
        var act = async () => await _sut.Handle(message, next, CancellationToken.None);

        await act.ShouldThrowAsync<InvalidOperationException>();
        
        _logger.Received().Log(
            LogLevel.Error,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("threw an unhandled exception")),
            exception,
            Arg.Any<Func<object, Exception?, string>>());
    }

    public sealed class TestMessage : INotification;
}