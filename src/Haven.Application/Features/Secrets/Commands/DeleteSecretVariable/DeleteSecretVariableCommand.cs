using Haven.Application.Common;
using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.Secrets.Commands.DeleteSecretVariable;

[RequirePermission(Permissions.ProjectManagement.ManageSecrets)]
public sealed class DeleteSecretVariableCommand : ICommand
{
    public Guid Id { get; set; }
}
