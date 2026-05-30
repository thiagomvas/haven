namespace Haven.Application.Common;

public static class Permissions
{
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
    }

    public static class Users
    {
        public const string Create = "users.create";
        public const string Update = "users.update";
        public const string Delete = "users.delete";
        public const string View = "users.view";
        public const string ManagePermissions = "users.manage_permissions";
    }
}
