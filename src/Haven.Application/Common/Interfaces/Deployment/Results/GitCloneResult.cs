namespace Haven.Application.Common.Interfaces.Deployment.Results;

public record GitCloneResult(
    bool Success,
    string RepositoryPath,
    string? ErrorMessage = null);
