using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Features.Secrets.Commands.CreateSecretVariable;
using Haven.Domain.Enums;

using NSubstitute;

using Shouldly;

namespace Haven.Application.Tests.Features.Secrets.Commands.CreateSecretVariable;

[Category("Unit")]
public sealed class CreateSecretVariableHandlerTests
{
    private ISecretVariableRepository _secretVariableRepository = null!;
    private CreateSecretVariableHandler _sut = null!;

    [SetUp]
    public void Setup()
    {
        _secretVariableRepository = Substitute.For<ISecretVariableRepository>();
        _sut = new CreateSecretVariableHandler(_secretVariableRepository);
    }

    [Test]
    public async Task Handle_WhenKeyIsUnique_ShouldCreateSecret()
    {
        var command = new CreateSecretVariableCommand
        {
            ParentId = Guid.NewGuid(),
            ParentType = EnvironmentVariableParentType.Service,
            Key = "API_KEY",
            Value = "super-secret"
        };
        _secretVariableRepository
            .ExistsWithKeyForParentAsync(command.ParentId, command.ParentType, command.Key, Guid.Empty, Arg.Any<CancellationToken>())
            .Returns(false);
        _secretVariableRepository
            .AddAsync(Arg.Any<Domain.Entities.SecretVariable>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.Arg<Domain.Entities.SecretVariable>().Id);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBe(Guid.Empty);
        await _secretVariableRepository.Received(1).AddAsync(
            Arg.Is<Domain.Entities.SecretVariable>(s =>
                s.ParentId == command.ParentId &&
                s.ParentType == command.ParentType &&
                s.Key == command.Key &&
                s.Value!.Value == command.Value),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WhenKeyAlreadyExistsForParent_ShouldReturnConflict()
    {
        var command = new CreateSecretVariableCommand
        {
            ParentId = Guid.NewGuid(),
            ParentType = EnvironmentVariableParentType.Project,
            Key = "API_KEY",
            Value = "super-secret"
        };
        _secretVariableRepository
            .ExistsWithKeyForParentAsync(command.ParentId, command.ParentType, command.Key, Guid.Empty, Arg.Any<CancellationToken>())
            .Returns(true);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
        result.Error.Code.ShouldBe("CONFLICT");
        await _secretVariableRepository.DidNotReceive().AddAsync(Arg.Any<Domain.Entities.SecretVariable>(), Arg.Any<CancellationToken>());
    }
}