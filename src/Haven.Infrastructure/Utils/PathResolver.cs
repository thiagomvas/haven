using Haven.Domain.Aggregates;
using Haven.Domain.Entities;
using Environment = Haven.Domain.Entities.Environment;

namespace Haven.Infrastructure.Utils;

public static class PathResolver
{
    private const string BasePath = "manifests";

    public static string ProjectsDirectory =>
        Path.Combine(BasePath, "projects");

    public static string ProjectPath(Project project) =>
        ProjectPath(project.Name);

    public static string ProjectPath(string projectName) =>
        Path.Combine(ProjectsDirectory, projectName);

    public static string ProjectFilePath(Project project) =>
        ProjectFilePath(project.Name);

    public static string ProjectFilePath(string projectName) =>
        Path.Combine(ProjectPath(projectName), "project.yaml");

    public static string EnvironmentPath(Project project, Environment environment) =>
        EnvironmentPath(project.Name, environment.Name);

    public static string EnvironmentPath(string projectName, string environmentName) =>
        Path.Combine(ProjectPath(projectName), "environments", environmentName);

    public static string EnvironmentFilePath(Project project, Environment environment) =>
        EnvironmentFilePath(project.Name, environment.Name);

    public static string EnvironmentFilePath(string projectName, string environmentName) =>
        Path.Combine(EnvironmentPath(projectName, environmentName), "environment.yaml");

    public static string ServicePath(Project project, Environment environment, Service service) =>
        ServicePath(project.Name, environment.Name, service.Name);

    public static string ServicePath(string projectName, string environmentName, string serviceName) =>
        Path.Combine(EnvironmentPath(projectName, environmentName), "services", serviceName);

    public static string ServiceFilePath(Project project, Environment environment, Service service) =>
        ServiceFilePath(project.Name, environment.Name, service.Name);

    public static string ServiceFilePath(string projectName, string environmentName, string serviceName) =>
        Path.Combine(ServicePath(projectName, environmentName, serviceName), "service.yaml");

    public static string NetworkFilePath(Project project, Environment environment) =>
        NetworkFilePath(project.Name, environment.Name);

    public static string NetworkFilePath(string projectName, string environmentName) =>
        Path.Combine(EnvironmentPath(projectName, environmentName), "network.yaml");
}