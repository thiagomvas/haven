using Haven.Application.Common;
using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.GitCredentials.Commands.DeleteGitCredentials;

[RequirePermission(Permissions.System.ManageGitCredentials)]
public sealed class DeleteGitCredentialsCommand : ICommand
{
    public Guid Id { get; set; }
}