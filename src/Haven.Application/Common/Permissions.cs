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


    public static class Projects
    {
        public const string Create = "projects.create";
        public const string Update = "projects.update";
        public const string Delete = "projects.delete";
        public const string View = "projects.view";
    }

    public static class Environments
    {
        public const string Create = "environments.create";
        public const string Update = "environments.update";
        public const string Delete = "environments.delete";
        public const string View = "environments.view";
    }

    public static class Services
    {
        public const string Create = "services.create";
        public const string Update = "services.update";
        public const string Delete = "services.delete";
        public const string View = "services.view";
        public const string Deploy = "services.deploy";
        public const string Operate = "services.operate";
    }

    public static class FeatureFlags
    {
        public const string Create = "feature_flags.create";
        public const string Update = "feature_flags.update";
        public const string Delete = "feature_flags.delete";
        public const string View = "feature_flags.view";
    }

    public static class Users
    {
        public const string Create = "users.create";
        public const string Update = "users.update";
        public const string Delete = "users.delete";
        public const string View = "users.view";
        public const string ManagePermissions = "users.manage_permissions";
    }

    public static class Credentials
    {
        public const string Create = "credentials.create";
        public const string Delete = "credentials.delete";
        public const string View = "credentials.view";
    }

    public static class Events
    {
        public const string View = "events.view";
    }
}
