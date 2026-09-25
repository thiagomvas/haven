using Haven.Application.Common;
using Haven.Application.Common.Messaging;
using Haven.Domain;

namespace Haven.Application.Features.Secrets.Commands.UpdateSecretVariable;

[RequirePermission(Permissions.ProjectManagement.ManageSecrets)]
public sealed class UpdateSecretVariableCommand : ICommand
{
    public Guid Id { get; set; }
    public Optional<string> Key { get; set; }
    public Optional<string> Value { get; set; }
}