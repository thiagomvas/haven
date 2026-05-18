using Haven.Application.Common;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Domain.Entities;
using Haven.Domain.ValueObjects;

namespace Haven.Application.Features.GitCredentials.Commands.CreateGitCredentials;
using GitCredentialsEntity = Haven.Domain.Entities.GitCredentials;

public sealed class CreateGitCredentialsHandler(IGitCredentialsRepository credentialsRepository)
    : Common.Messaging.ICommandHandler<CreateGitCredentialsCommand, Guid>
{
    public async ValueTask<Result<Guid>> Handle(CreateGitCredentialsCommand request, CancellationToken cancellationToken)
    {
        var exists = await credentialsRepository.ExistsWithDisplayNameAsync(
            request.DisplayName,
            Guid.Empty,
            cancellationToken);

        if (exists)
            return Error.ConflictFor(nameof(GitCredentialsEntity), request.DisplayName);

        var credentials = GitCredentialsEntity.Create(
            request.ProviderType,
            request.HostUrl,
            request.AuthMethod,
            EncryptedValue.From(request.PrimaryCredential),
            request.SecondaryCredential != null ? EncryptedValue.From(request.SecondaryCredential) : null,
            request.WebhookSecret,
            request.DisplayName);

        var credentialsId = await credentialsRepository.AddAsync(credentials, cancellationToken);

        return Result<Guid>.CreatedFor(credentialsId);
    }
}
