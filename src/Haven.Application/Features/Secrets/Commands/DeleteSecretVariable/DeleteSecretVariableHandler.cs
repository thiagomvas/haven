using Haven.Application.Common;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Messaging;
using Haven.Domain.Entities;

namespace Haven.Application.Features.Secrets.Commands.DeleteSecretVariable;

public sealed class DeleteSecretVariableHandler(ISecretVariableRepository secretVariableRepository)
    : ICommandHandler<DeleteSecretVariableCommand>
{
    public async ValueTask<Result> Handle(DeleteSecretVariableCommand request, CancellationToken cancellationToken)
    {
        var secret = await secretVariableRepository.GetByIdAsync(request.Id, cancellationToken);
        if (secret is null)
            return Error.NotFoundFor(nameof(SecretVariable), request.Id);

        secretVariableRepository.Remove(secret);

        return Result.Success();
    }
}
