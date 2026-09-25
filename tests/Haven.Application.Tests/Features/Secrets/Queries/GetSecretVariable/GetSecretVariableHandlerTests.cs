using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Features.Secrets.Queries.GetSecretVariable;
using Haven.Domain.Entities;
using Haven.Domain.Enums;
using Haven.Domain.ValueObjects;

using NSubstitute;

using Shouldly;

namespace Haven.Application.Tests.Features.Secrets.Queries.GetSecretVariable;

[Category("Unit")]
public sealed class GetSecretVariableHandlerTests
{
    private ISecretVariableRepository _secretVariableRepository = null!;
    private GetSecretVariableHandler _sut = null!;

    [SetUp]
    public void Setup()
    {
        _secretVariableRepository = Substitute.For<ISecretVariableRepository>();
        _sut = new GetSecretVariableHandler(_secretVariableRepository);
    }

    [Test]
    public async Task Handle_WhenSecretDoesNotExist_ShouldReturnNotFound()
    {
        var query = new GetSecretVariableQuery { Id = Guid.NewGuid() };
        _secretVariableRepository.GetByIdAsync(query.Id, Arg.Any<CancellationToken>()).Returns((SecretVariable?)null);

        var result = await _sut.Handle(query, CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
        result.Error.Code.ShouldBe("NOT_FOUND");
    }

    [Test]
    public async Task Handle_WhenSecretExists_ShouldReturnDto_WithoutExposingDecryptedValue()
    {
        var secret = new SecretVariable
        {
            ParentId = Guid.NewGuid(),
            ParentType = EnvironmentVariableParentType.Project,
            Key = "API_KEY",
            Value = EncryptedValue.From("super-secret")
        };
        var query = new GetSecretVariableQuery { Id = secret.Id };
        _secretVariableRepository.GetByIdAsync(query.Id, Arg.Any<CancellationToken>()).Returns(secret);

        var result = await _sut.Handle(query, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Id.ShouldBe(secret.Id);
        result.Value.ParentId.ShouldBe(secret.ParentId);
        result.Value.ParentType.ShouldBe(secret.ParentType);
        result.Value.Key.ShouldBe(secret.Key);
        result.Value.HasValue.ShouldBeTrue();
    }
}