namespace Haven.Application.Features.Services.ComputedOutputs;

public sealed record ResolvedComputedOutput(
    string Key,
    string Label,
    bool IsSecret,
    bool IsAvailable,
    string? UnavailableReason,
    string? Value,
    string? MaskedPreview);