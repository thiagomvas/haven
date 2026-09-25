using Haven.Application.Common;
using Haven.Application.Common.Messaging;
using Haven.Domain.Enums;

namespace Haven.Application.Features.Secrets.Queries.GetSecretVariablesForParent;

[RequirePermission(Permissions.ProjectManagement.ManageSecrets)]
public sealed class GetSecretVariablesForParentQuery : PagedQuery<SecretVariableDto>
{
    public Guid ParentId { get; set; }
    public EnvironmentVariableParentType ParentType { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; } = 100;
}