using Haven.Application.Common;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Messaging;
using Haven.Domain.Entities;
using Haven.Domain.ValueObjects;

namespace Haven.Application.Features.Secrets.Commands.CreateSecretVariable;

public sealed class CreateSecretVariableHandler(ISecretVariableRepository secretVariableRepository)
    : ICommandHandler<CreateSecretVariableCommand, Guid>
{
    public async ValueTask<Result<Guid>> Handle(CreateSecretVariableCommand request, CancellationToken cancellationToken)
    {
        var exists = await secretVariableRepository.ExistsWithKeyForParentAsync(
            request.ParentId,
            request.ParentType,
            request.Key,
            Guid.Empty,
            cancellationToken);

        if (exists)
            return Error.ConflictFor(nameof(SecretVariable), request.Key);

        var secret = new SecretVariable
        {
            ParentId = request.ParentId,
            ParentType = request.ParentType,
            Key = request.Key,
            Value = EncryptedValue.From(request.Value)
        };

        var id = await secretVariableRepository.AddAsync(secret, cancellationToken);

        return Result<Guid>.CreatedFor(id);
    }
}