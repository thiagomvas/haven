using Haven.Application.Features.GitCredentials;
using Haven.Domain.Entities;
using Riok.Mapperly.Abstractions;

namespace Haven.Application.Mappers;

[Mapper(UseDeepCloning = true, RequiredMappingStrategy = RequiredMappingStrategy.None)]
public static partial class GitCredentialMapper
{
    private static partial GitCredentialDto ToDtoPartial(this GitCredentials credentials);

    public static GitCredentialDto ToDto(this GitCredentials credentials)
    {
        return credentials.ToDtoPartial();
    }
}