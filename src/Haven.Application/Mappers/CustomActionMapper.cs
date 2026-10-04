using Haven.Application.Features.CustomActions;
using Haven.Domain.Entities;

using Riok.Mapperly.Abstractions;

namespace Haven.Application.Mappers;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.None)]
public static partial class CustomActionMapper
{
    public static partial CustomActionDto ToDto(this CustomAction action);

    public static partial IReadOnlyList<CustomActionDto> ToDtos(this IEnumerable<CustomAction> actions);
}
