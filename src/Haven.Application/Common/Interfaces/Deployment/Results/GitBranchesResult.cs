namespace Haven.Application.Common.Interfaces.Deployment.Results;

public record GitBranchesResult(
    bool Success,
    IReadOnlyList<string> Branches = null!,
    string? ErrorMessage = null);
