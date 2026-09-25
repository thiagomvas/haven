using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Messaging;
using Haven.Application.Features.Secrets.Queries.GetSecretVariablesForParent;
using Haven.Domain.Entities;
using Haven.Domain.Enums;
using Haven.Domain.ValueObjects;

using NSubstitute;

using Shouldly;

namespace Haven.Application.Tests.Features.Secrets.Queries.GetSecretVariablesForParent;

[Category("Unit")]
public sealed class GetSecretVariablesForParentHandlerTests
{
    private ISecretVariableRepository _secretVariableRepository = null!;
    private GetSecretVariablesForParentHandler _sut = null!;

    [SetUp]
    public void Setup()
    {
        _secretVariableRepository = Substitute.For<ISecretVariableRepository>();
        _sut = new GetSecretVariablesForParentHandler(_secretVariableRepository);
    }

    [Test]
    public async Task Handle_ShouldReturnPagedDtos_WithoutExposingDecryptedValues()
    {
        var parentId = Guid.NewGuid();
        var query = new GetSecretVariablesForParentQuery
        {
            ParentId = parentId,
            ParentType = EnvironmentVariableParentType.Service,
            PageNumber = 1,
            PageSize = 50
        };
        var secrets = new List<SecretVariable>
        {
            new()
            {
                ParentId = parentId,
                ParentType = EnvironmentVariableParentType.Service,
                Key = "KEY_ONE",
                Value = EncryptedValue.From("value-one")
            },
            new()
            {
                ParentId = parentId,
                ParentType = EnvironmentVariableParentType.Service,
                Key = "KEY_TWO",
                Value = null
            }
        };
        _secretVariableRepository
            .GetForParentPagedAsync(parentId, EnvironmentVariableParentType.Service, 1, 50, Arg.Any<CancellationToken>())
            .Returns(new PagedResult<SecretVariable>(secrets, secrets.Count, 1, 50));

        var result = await _sut.Handle(query, CancellationToken.None);

        result.Items.Count.ShouldBe(2);
        result.Items[0].Key.ShouldBe("KEY_ONE");
        result.Items[0].HasValue.ShouldBeTrue();
        result.Items[1].Key.ShouldBe("KEY_TWO");
        result.Items[1].HasValue.ShouldBeFalse();
    }
}
