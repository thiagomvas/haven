using Haven.Application.Features.Services.ComputedOutputs;
using Haven.Application.Features.Services.Queries;

namespace Haven.Application.Mappers;

public static class ComputedOutputMapper
{
    public static ComputedOutputDto ToDto(this ResolvedComputedOutput output) => new()
    {
        Key = output.Key,
        Label = output.Label,
        IsSecret = output.IsSecret,
        IsAvailable = output.IsAvailable,
        UnavailableReason = output.UnavailableReason,
        Preview = output.MaskedPreview
    };

    public static List<ComputedOutputDto> ToDtos(this IEnumerable<ResolvedComputedOutput> outputs) =>
        outputs.Select(ToDto).ToList();
}
