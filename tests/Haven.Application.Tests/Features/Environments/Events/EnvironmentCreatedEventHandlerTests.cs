using Haven.Application.Common;
using Haven.Application.Features.Environments.Events;
using Haven.Application.Features.Networks.Commands.CreateNetwork;
using Haven.Domain.Aggregates;
using Haven.Domain.Events;
using Mediator;
using NSubstitute;
using Shouldly;

namespace Haven.Application.Tests.Features.Environments.Events;

[TestFixture]
[Category("Unit")]
public sealed class EnvironmentCreatedEventHandlerTests
{
    private IMediator _mediator = null!;
    private EnvironmentCreatedEventHandler _sut = null!;

    [SetUp]
    public void Setup()
    {
        _mediator = Substitute.For<IMediator>();
        _sut = new EnvironmentCreatedEventHandler(_mediator);
    }

    [Test]
    public async Task Handle_SendsCreateNetworkCommand()
    {
        var project = Project.Create("MyProject");
        var environment = project.AddEnvironment("staging");
        var notification = new EnvironmentCreatedEvent(project, environment);

        await _sut.Handle(notification, CancellationToken.None);

        await _mediator.Received(1).Send(
            Arg.Is<CreateNetworkCommand>(cmd =>
                cmd.ProjectId == project.Id &&
                cmd.EnvironmentId == environment.Id),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_CommandNameIncludesProjectAndEnvironmentNames()
    {
        var project = Project.Create("TestProject");
        var environment = project.AddEnvironment("production");
        var notification = new EnvironmentCreatedEvent(project, environment);

        await _sut.Handle(notification, CancellationToken.None);

        await _mediator.Received(1).Send(
            Arg.Is<CreateNetworkCommand>(cmd =>
                cmd.Name.ToLowerInvariant().Contains("testproject") &&
                cmd.Name.ToLowerInvariant().Contains("production")),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_CommandNameIsConsistent()
    {
        var project = Project.Create("MyProject");
        var environment = project.AddEnvironment("staging");
        var notification = new EnvironmentCreatedEvent(project, environment);

        CreateNetworkCommand? capturedCommand = null;
        _mediator.Send(Arg.Do<CreateNetworkCommand>(cmd => capturedCommand = cmd), Arg.Any<CancellationToken>())
            .Returns(Result<Guid>.Success(Guid.NewGuid()));

        await _sut.Handle(notification, CancellationToken.None);

        capturedCommand.ShouldNotBeNull();
        var expectedName = Haven.Domain.Aggregates.Network.CreateProjectEnvironmentNetwork(
            project.Id,
            project.Name,
            environment.Id,
            environment.Name).Name;

        capturedCommand.Name.ShouldBe(expectedName);
    }
}
