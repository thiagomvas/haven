using Haven.Application.Common.Interfaces.Deployment;

namespace Haven.Infrastructure.Deployment.Git;

public sealed class GitRepositoryPathProvider(string baseRepositoryPath) : IGitRepositoryPathProvider
{
    private readonly string _baseRepositoryPath = baseRepositoryPath.TrimEnd(Path.DirectorySeparatorChar);

    public string GetRepositoryRootPath() => _baseRepositoryPath;

    public string GetServiceRepositoryPath(Guid serviceId) =>
        Path.Combine(_baseRepositoryPath, "services", serviceId.ToString());

    public async Task EnsureRepositoryDirectoryExistsAsync(Guid serviceId, CancellationToken cancellationToken = default)
    {
        var path = GetServiceRepositoryPath(serviceId);
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }

        await Task.CompletedTask;
    }

    public bool RepositoryDirectoryExists(Guid serviceId)
    {
        var path = GetServiceRepositoryPath(serviceId);
        return Directory.Exists(path);
    }
}