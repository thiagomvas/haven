using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Features.Secrets.Commands.DeleteSecretVariable;
using Haven.Domain.Entities;
using Haven.Domain.Enums;
using Haven.Domain.ValueObjects;

using NSubstitute;

using Shouldly;

namespace Haven.Application.Tests.Features.Secrets.Commands.DeleteSecretVariable;

[Category("Unit")]
public sealed class DeleteSecretVariableHandlerTests
{
    private ISecretVariableRepository _secretVariableRepository = null!;
    private DeleteSecretVariableHandler _sut = null!;

    [SetUp]
    public void Setup()
    {
        _secretVariableRepository = Substitute.For<ISecretVariableRepository>();
        _sut = new DeleteSecretVariableHandler(_secretVariableRepository);
    }

    [Test]
    public async Task Handle_WhenSecretDoesNotExist_ShouldReturnNotFound()
    {
        var command = new DeleteSecretVariableCommand { Id = Guid.NewGuid() };
        _secretVariableRepository.GetByIdAsync(command.Id, Arg.Any<CancellationToken>()).Returns((SecretVariable?)null);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
        result.Error.Code.ShouldBe("NOT_FOUND");
        _secretVariableRepository.DidNotReceive().Remove(Arg.Any<SecretVariable>());
    }

    [Test]
    public async Task Handle_WhenSecretExists_ShouldRemoveIt()
    {
        var secret = new SecretVariable
        {
            ParentId = Guid.NewGuid(),
            ParentType = EnvironmentVariableParentType.Environment,
            Key = "KEY",
            Value = EncryptedValue.From("value")
        };
        var command = new DeleteSecretVariableCommand { Id = secret.Id };
        _secretVariableRepository.GetByIdAsync(command.Id, Arg.Any<CancellationToken>()).Returns(secret);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        _secretVariableRepository.Received(1).Remove(secret);
    }
}