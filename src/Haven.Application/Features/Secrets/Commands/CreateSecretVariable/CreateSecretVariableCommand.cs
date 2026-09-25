using Haven.Application.Common;
using Haven.Application.Common.Messaging;
using Haven.Domain.Enums;

namespace Haven.Application.Features.Secrets.Commands.CreateSecretVariable;

[RequirePermission(Permissions.ProjectManagement.ManageSecrets)]
public sealed class CreateSecretVariableCommand : ICommand<Guid>
{
    public Guid ParentId { get; set; }
    public EnvironmentVariableParentType ParentType { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}