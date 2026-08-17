using Haven.Application.Configuration;
using Haven.Domain.Aggregates;
using Haven.Domain.Entities;
using Haven.Domain.Enums;

using Microsoft.Extensions.Options;

using Environment = Haven.Domain.Aggregates.Environment;

namespace Haven.Infrastructure.Utils;

public static class PathResolver
{
    public const string ProjectFile = "project.yaml";
    public const string EnvironmentDirectory = "environments";
    public const string EnvironmentFile = "environment.yaml";
    public const string ServiceDirectory = "services";
    public const string ServiceFile = "service.yaml";
    public const string NetworkFile = "network.yaml";
    public const string NetworksDirectory = "networks";
    public const string SidecarsDirectory = "sidecars";
    public const string EnvExampleFile = ".env.example";
    private static IOptionsMonitor<ManifestsOptions>? _optionsMonitor;

    public static void Initialize(IOptionsMonitor<ManifestsOptions> optionsMonitor)
    {
        _optionsMonitor = optionsMonitor;
    }

    private static string BasePath =>
        _optionsMonitor?.CurrentValue.ManifestsPath ?? "manifests";

    public static string ProjectsDirectory =>
        Path.Combine(BasePath, "projects");

    public static string ProjectPath(Project project) =>
        ProjectPath(project.Name);

    public static string ProjectPath(string projectName) =>
        Path.Combine(ProjectsDirectory, projectName);

    public static string ProjectFilePath(Project project) =>
        ProjectFilePath(project.Name);

    public static string ProjectFilePath(string projectName) =>
        Path.Combine(ProjectPath(projectName), ProjectFile);
    public static string ProjectFilePathForDirectory(string projectDirectory) =>
        Path.Combine(projectDirectory, ProjectFile);

    public static string EnvironmentPath(Project project, Environment environment) =>
        EnvironmentPath(project.Name, environment.Name);

    public static string EnvironmentPath(string projectName, string environmentName) =>
        Path.Combine(ProjectPath(projectName), EnvironmentDirectory, environmentName);

    public static string EnvironmentPath(string projectName) =>
        Path.Combine(ProjectPath(projectName), EnvironmentDirectory);

    public static string EnvironmentFilePath(Project project, Environment environment) =>
        EnvironmentFilePath(project.Name, environment.Name);

    public static string EnvironmentFilePath(string projectName, string environmentName) =>
        Path.Combine(EnvironmentPath(projectName, environmentName), EnvironmentFile);

    public static string ServicePath(Project project, Environment environment, Service service) =>
        ServicePath(project.Name, environment.Name, service.Name);

    public static string ServicePath(string projectName, string environmentName, string serviceName) =>
        Path.Combine(EnvironmentPath(projectName, environmentName), ServiceDirectory, serviceName);

    public static string ServiceFilePath(Project project, Environment environment, Service service) =>
        ServiceFilePath(project.Name, environment.Name, service.Name);

    public static string ServiceFilePath(string projectName, string environmentName, string serviceName) =>
        Path.Combine(ServicePath(projectName, environmentName, serviceName), ServiceFile);

    public static string NetworkFilePath(Project project, Environment environment) =>
        NetworkFilePath(project.Name, environment.Name);

    public static string NetworkFilePath(string projectName, string environmentName) =>
        Path.Combine(EnvironmentPath(projectName, environmentName), NetworkFile);

    public static string NetworksDirectoryPath =>
        Path.Combine(BasePath, NetworksDirectory);

    public static string SharedNetworkFilePath(Guid networkId) =>
        Path.Combine(NetworksDirectoryPath, $"{networkId}.yaml");

    public static string SharedNetworkFilePath(string basePath, Guid networkId) =>
        Path.Combine(basePath, NetworksDirectory, $"{networkId}.yaml");

    public static string SidecarsDirectoryPath =>
        Path.Combine(BasePath, SidecarsDirectory);

    // Sidecars are keyed by Kind, not Id, since built-in sidecars are unique per Kind.
    public static string SidecarFilePath(SidecarKind kind) =>
        Path.Combine(SidecarsDirectoryPath, $"{kind.ToString().ToLowerInvariant()}.yaml");

    public static string SidecarFilePath(string basePath, SidecarKind kind) =>
        Path.Combine(basePath, SidecarsDirectory, $"{kind.ToString().ToLowerInvariant()}.yaml");

    public static string ProjectEnvExamplePath(Project project) =>
        ProjectEnvExamplePath(project.Name);

    public static string ProjectEnvExamplePath(string projectName) =>
        Path.Combine(ProjectPath(projectName), EnvExampleFile);

    public static string EnvironmentEnvExamplePath(Project project, Environment environment) =>
        EnvironmentEnvExamplePath(project.Name, environment.Name);

    public static string EnvironmentEnvExamplePath(string projectName, string environmentName) =>
        Path.Combine(EnvironmentPath(projectName, environmentName), EnvExampleFile);

    public static string ServiceEnvExamplePath(Project project, Environment environment, Service service) =>
        ServiceEnvExamplePath(project.Name, environment.Name, service.Name);

    public static string ServiceEnvExamplePath(string projectName, string environmentName, string serviceName) =>
        Path.Combine(ServicePath(projectName, environmentName, serviceName), EnvExampleFile);

    public static string ProjectEnvExamplePath(string basePath, string projectName) =>
        Path.Combine(basePath, "projects", projectName, EnvExampleFile);

    public static string EnvironmentEnvExamplePath(string basePath, string projectName, string environmentName) =>
        Path.Combine(basePath, "projects", projectName, EnvironmentDirectory, environmentName, EnvExampleFile);

    public static string ServiceEnvExamplePath(string basePath, string projectName, string environmentName, string serviceName) =>
        Path.Combine(basePath, "projects", projectName, EnvironmentDirectory, environmentName, ServiceDirectory, serviceName, EnvExampleFile);
}