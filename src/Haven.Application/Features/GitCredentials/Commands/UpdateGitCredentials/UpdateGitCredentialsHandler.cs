using Haven.Application.Common;
using Haven.Application.Common.Interfaces.Repositories;

namespace Haven.Application.Features.GitCredentials.Commands.UpdateGitCredentials;

using GitCredentialsEntity = Haven.Domain.Entities.GitCredentials;

public sealed class UpdateGitCredentialsHandler(IGitCredentialsRepository credentialsRepository)
    : Common.Messaging.ICommandHandler<UpdateGitCredentialsCommand, Guid>
{
    public async ValueTask<Result<Guid>> Handle(UpdateGitCredentialsCommand request, CancellationToken cancellationToken)
    {
        var credentials = await credentialsRepository.GetByIdAsync(request.Id, cancellationToken);
        if (credentials is null)
            return Error.NotFoundFor(nameof(GitCredentialsEntity), request.Id);

        if (request.DisplayName.HasValue)
        {
            var exists = await credentialsRepository.ExistsWithDisplayNameAsync(
                request.DisplayName.Value,
                request.Id,
                cancellationToken);

            if (exists)
                return Error.ConflictFor(nameof(GitCredentialsEntity), request.DisplayName.Value);
        }

        credentials.Update(request.DisplayName, request.IsActive);

        return Result<Guid>.Success(credentials.Id);
    }
}