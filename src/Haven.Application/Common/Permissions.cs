using System.Reflection;

namespace Haven.Application.Common;

public static class Permissions
{
    private static IReadOnlyList<string>? _all;

    public static IReadOnlyList<string> All => _all ??= typeof(Permissions)
        .GetNestedTypes(BindingFlags.Public | BindingFlags.Static)
        .SelectMany(t => t.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy))
        .Where(f => f.IsLiteral && f.FieldType == typeof(string))
        .Select(f => (string)f.GetRawConstantValue()!)
        .ToList()
        .AsReadOnly();

    public static class ProjectManagement
    {
        /// <summary>
        /// Any read operation related to Projects, Environments and Services
        /// </summary>
        public const string Read = "projects.read";
        
        /// <summary>
        /// Any operations related to the lifecycle of the services, such as Deploying, Restarting, Stopping...
        /// </summary>
        public const string ManageDeploys = "projects.manage_deploys";
        
        /// <summary>
        /// Any operation related to configuring the project, such as managing environment variables, feature flags, container info, etc
        /// </summary>
        public const string ManageConfig = "projects.manage_config";
        
        /// <summary>
        /// Any operation related to configuring secrets for the project.
        /// </summary>
        public const string ManageSecrets = "projects.manage_secrets";
        
        /// <summary>
        /// Any operation related to the project itself, such as creating or updating information
        /// </summary>
        public const string Create = "projects.create";
        
        /// <summary>
        /// Any destructive operation related to deleting an entry related to a project
        /// </summary>
        public const string Delete = "projects.delete";
    }

    public static class Dns
    {
        public const string Read = "dns.read";
        public const string ManageNetworks = "dns.manage_networks";
    }

    public static class System
    {
        public const string ReadGitCredentials = "system.read_git_credentials";
        public const string ManageGitCredentials = "system.manage_git_credentials";
        public const string ReadUsers = "system.read_users";
        public const string ManageUsers = "system.manage_users";
    }
}