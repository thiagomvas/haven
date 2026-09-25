using Haven.Application.Common;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Messaging;
using Haven.Application.Mappers;
using Haven.Domain.Entities;

namespace Haven.Application.Features.Secrets.Queries.GetSecretVariable;

public sealed class GetSecretVariableHandler(ISecretVariableRepository secretVariableRepository)
    : IQueryHandler<GetSecretVariableQuery, SecretVariableDto>
{
    public async ValueTask<Result<SecretVariableDto>> Handle(GetSecretVariableQuery request, CancellationToken cancellationToken)
    {
        var secret = await secretVariableRepository.GetByIdAsync(request.Id, cancellationToken);
        if (secret is null)
            return Error.NotFoundFor(nameof(SecretVariable), request.Id);

        return Result<SecretVariableDto>.Success(secret.ToDto());
    }
}