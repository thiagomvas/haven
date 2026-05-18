namespace Haven.Application.Common.Interfaces.Deployment.Results;

public record GitPullResult(
    bool Success,
    string? CurrentBranch = null,
    int? CommitsAhead = null,
    string? ErrorMessage = null);
