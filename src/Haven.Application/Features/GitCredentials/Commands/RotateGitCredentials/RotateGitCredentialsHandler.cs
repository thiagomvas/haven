using Haven.Application.Common;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Domain.ValueObjects;

namespace Haven.Application.Features.GitCredentials.Commands.RotateGitCredentials;

using GitCredentialsEntity = Haven.Domain.Entities.GitCredentials;

public sealed class RotateGitCredentialsHandler(IGitCredentialsRepository credentialsRepository)
    : Common.Messaging.ICommandHandler<RotateGitCredentialsCommand, Guid>
{
    public async ValueTask<Result<Guid>> Handle(RotateGitCredentialsCommand request, CancellationToken cancellationToken)
    {
        var credentials = await credentialsRepository.GetByIdAsync(request.Id, cancellationToken);
        if (credentials is null)
            return Error.NotFoundFor(nameof(GitCredentialsEntity), request.Id);

        credentials.RotateManualCredential(
            request.AuthMethod,
            EncryptedValue.From(request.PrimaryCredential),
            request.SecondaryCredential != null ? EncryptedValue.From(request.SecondaryCredential) : null,
            request.WebhookSecret);

        return Result<Guid>.Success(credentials.Id);
    }
}