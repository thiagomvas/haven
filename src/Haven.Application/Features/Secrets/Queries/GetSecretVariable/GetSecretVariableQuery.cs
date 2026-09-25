using Haven.Application.Common;
using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.Secrets.Queries.GetSecretVariable;

[RequirePermission(Permissions.ProjectManagement.ManageSecrets)]
public sealed class GetSecretVariableQuery : IQuery<SecretVariableDto>
{
    public Guid Id { get; set; }
}
