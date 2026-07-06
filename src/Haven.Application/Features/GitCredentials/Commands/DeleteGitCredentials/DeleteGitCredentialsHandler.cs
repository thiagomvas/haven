using Haven.Application.Common;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.GitCredentials.Commands.DeleteGitCredentials;

using GitCredentialsEntity = Haven.Domain.Entities.GitCredentials;

public sealed class DeleteGitCredentialsHandler(IGitCredentialsRepository credentialsRepository)
    : ICommandHandler<DeleteGitCredentialsCommand>
{
    public async ValueTask<Result> Handle(DeleteGitCredentialsCommand request, CancellationToken cancellationToken)
    {
        var credentials = await credentialsRepository.GetByIdAsync(request.Id, cancellationToken);
        if (credentials is null)
            return Error.NotFoundFor(nameof(GitCredentialsEntity), request.Id);

        credentialsRepository.Remove(credentials);

        return Result.Success();
    }
}
