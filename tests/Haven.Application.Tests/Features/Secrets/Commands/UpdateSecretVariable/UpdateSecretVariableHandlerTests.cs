using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Features.Secrets.Commands.UpdateSecretVariable;
using Haven.Domain.Entities;
using Haven.Domain.Enums;
using Haven.Domain.ValueObjects;

using NSubstitute;

using Shouldly;

namespace Haven.Application.Tests.Features.Secrets.Commands.UpdateSecretVariable;

[Category("Unit")]
public sealed class UpdateSecretVariableHandlerTests
{
    private ISecretVariableRepository _secretVariableRepository = null!;
    private UpdateSecretVariableHandler _sut = null!;

    [SetUp]
    public void Setup()
    {
        _secretVariableRepository = Substitute.For<ISecretVariableRepository>();
        _sut = new UpdateSecretVariableHandler(_secretVariableRepository);
    }

    [Test]
    public async Task Handle_WhenSecretDoesNotExist_ShouldReturnNotFound()
    {
        var command = new UpdateSecretVariableCommand { Id = Guid.NewGuid(), Key = "NEW_KEY" };
        _secretVariableRepository.GetByIdAsync(command.Id, Arg.Any<CancellationToken>()).Returns((SecretVariable?)null);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
        result.Error.Code.ShouldBe("NOT_FOUND");
    }

    [Test]
    public async Task Handle_WhenKeyProvidedAndAlreadyUsedByAnotherSecret_ShouldReturnConflict()
    {
        var secret = CreateSecret();
        var command = new UpdateSecretVariableCommand { Id = secret.Id, Key = "TAKEN_KEY" };
        _secretVariableRepository.GetByIdAsync(command.Id, Arg.Any<CancellationToken>()).Returns(secret);
        _secretVariableRepository
            .ExistsWithKeyForParentAsync(secret.ParentId, secret.ParentType, "TAKEN_KEY", secret.Id, Arg.Any<CancellationToken>())
            .Returns(true);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
        result.Error.Code.ShouldBe("CONFLICT");
        secret.Key.ShouldNotBe("TAKEN_KEY");
    }

    [Test]
    public async Task Handle_WhenKeyProvidedAndUnique_ShouldUpdateKey()
    {
        var secret = CreateSecret();
        var command = new UpdateSecretVariableCommand { Id = secret.Id, Key = "NEW_KEY" };
        _secretVariableRepository.GetByIdAsync(command.Id, Arg.Any<CancellationToken>()).Returns(secret);
        _secretVariableRepository
            .ExistsWithKeyForParentAsync(secret.ParentId, secret.ParentType, "NEW_KEY", secret.Id, Arg.Any<CancellationToken>())
            .Returns(false);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        secret.Key.ShouldBe("NEW_KEY");
    }

    [Test]
    public async Task Handle_WhenValueProvided_ShouldUpdateValue()
    {
        var secret = CreateSecret();
        var command = new UpdateSecretVariableCommand { Id = secret.Id, Value = "new-secret-value" };
        _secretVariableRepository.GetByIdAsync(command.Id, Arg.Any<CancellationToken>()).Returns(secret);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        secret.Value!.Value.ShouldBe("new-secret-value");
    }

    [Test]
    public async Task Handle_WhenNeitherKeyNorValueProvided_ShouldNotChangeSecret()
    {
        var secret = CreateSecret();
        var command = new UpdateSecretVariableCommand { Id = secret.Id };
        _secretVariableRepository.GetByIdAsync(command.Id, Arg.Any<CancellationToken>()).Returns(secret);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        secret.Key.ShouldBe("ORIGINAL_KEY");
        secret.Value!.Value.ShouldBe("original-value");
    }

    private static SecretVariable CreateSecret() => new()
    {
        ParentId = Guid.NewGuid(),
        ParentType = EnvironmentVariableParentType.Service,
        Key = "ORIGINAL_KEY",
        Value = EncryptedValue.From("original-value")
    };
}
