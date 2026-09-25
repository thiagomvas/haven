using Haven.Application.Features.Secrets;
using Haven.Domain.Entities;

using Riok.Mapperly.Abstractions;

namespace Haven.Application.Mappers;

[Mapper(UseDeepCloning = true, RequiredMappingStrategy = RequiredMappingStrategy.None)]
public static partial class SecretVariableMapper
{
    private static partial SecretVariableDto ToDtoPartial(this SecretVariable secret);

    public static SecretVariableDto ToDto(this SecretVariable secret)
    {
        var dto = secret.ToDtoPartial();
        dto.HasValue = secret.Value is not null;
        return dto;
    }
}
