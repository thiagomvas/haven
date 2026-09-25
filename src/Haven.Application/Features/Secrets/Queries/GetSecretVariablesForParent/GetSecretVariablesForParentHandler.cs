using Haven.Application.Common;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Messaging;
using Haven.Application.Mappers;

namespace Haven.Application.Features.Secrets.Queries.GetSecretVariablesForParent;

public sealed class GetSecretVariablesForParentHandler(ISecretVariableRepository secretVariableRepository)
    : IPagedQueryHandler<GetSecretVariablesForParentQuery, SecretVariableDto>
{
    public async ValueTask<PagedResult<SecretVariableDto>> Handle(GetSecretVariablesForParentQuery query, CancellationToken cancellationToken)
    {
        var result = await secretVariableRepository.GetForParentPagedAsync(
            query.ParentId,
            query.ParentType,
            query.PageNumber,
            query.PageSize,
            cancellationToken);

        return result.Project(s => s.ToDto());
    }
}