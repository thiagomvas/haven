using Haven.Application.Common;
using Haven.Application.Common.Messaging;
using Haven.Domain;

namespace Haven.Application.Features.GitCredentials.Commands.UpdateGitCredentials;

[RequirePermission(Permissions.System.ManageGitCredentials)]
public sealed class UpdateGitCredentialsCommand : ICommand<Guid>
{
    public Guid Id { get; set; }
    public Optional<string> DisplayName { get; set; }
    public Optional<bool> IsActive { get; set; }
}