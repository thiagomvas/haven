namespace Haven.Application.Features.Services.Shared;

public sealed record BulkServiceActionResult(Guid ServiceId, bool Success, string? ErrorMessage);

public sealed record BulkServiceActionResponse(IReadOnlyList<BulkServiceActionResult> Results)
{
    public int SucceededCount => Results.Count(r => r.Success);
    public int FailedCount => Results.Count(r => !r.Success);
}
