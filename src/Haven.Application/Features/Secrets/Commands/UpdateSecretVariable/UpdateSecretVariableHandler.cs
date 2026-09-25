using Haven.Application.Common;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Messaging;
using Haven.Domain.Entities;
using Haven.Domain.ValueObjects;

namespace Haven.Application.Features.Secrets.Commands.UpdateSecretVariable;

public sealed class UpdateSecretVariableHandler(ISecretVariableRepository secretVariableRepository)
    : ICommandHandler<UpdateSecretVariableCommand>
{
    public async ValueTask<Result> Handle(UpdateSecretVariableCommand request, CancellationToken cancellationToken)
    {
        var secret = await secretVariableRepository.GetByIdAsync(request.Id, cancellationToken);
        if (secret is null)
            return Error.NotFoundFor(nameof(SecretVariable), request.Id);

        if (request.Key.HasValue)
        {
            var exists = await secretVariableRepository.ExistsWithKeyForParentAsync(
                secret.ParentId,
                secret.ParentType,
                request.Key.Value,
                request.Id,
                cancellationToken);

            if (exists)
                return Error.ConflictFor(nameof(SecretVariable), request.Key.Value);

            secret.Key = request.Key.Value;
        }

        if (request.Value.HasValue)
            secret.Value = EncryptedValue.From(request.Value.Value);

        return Result.Success();
    }
}
