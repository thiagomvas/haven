namespace Haven.Application.Common.Templating;

public static class TemplateNamespaces
{
    /// <summary>
    /// Namespace for template input variables, which are provided by the user when instantiating a template.
    /// </summary>
    public const string Inputs = "inputs";

    /// <summary>
    /// Namespace for environment variables.
    /// </summary>
    public const string EnvVariables = "env";

    /// <summary>
    /// Namespace for secret variables.
    /// </summary>
    public const string Secrets = "secrets";

    /// <summary>
    /// Namespace for runtime variables, which are provided by the system at runtime.
    /// </summary>
    public const string Runtime = "runtime";
    
    /// <summary>
    /// Namespace for output variables, which are provided by the system after the template has been instantiated.
    /// </summary>
    public const string Outputs = "outputs";
    
    /// <summary>
    /// Namespace for variables in services within the same environment, which are provided by the system at runtime.
    /// </summary>
    public const string Services = "services";

    /// <summary>
    /// The keys resolvable within the <see cref="Runtime"/> namespace, with a short description of each.
    /// Kept next to the namespace names so editor suggestions can't drift from what the resolver supports.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, string> RuntimeKeys = new Dictionary<string, string>
    {
        ["id"] = "The service's ID",
        ["alias"] = "The service's alias",
        ["name"] = "The service's name",
        ["environment"] = "The name of the environment the service belongs to",
        ["project"] = "The name of the project the service belongs to",
        ["hostname"] = "The container hostname other services can reach this service on",
        ["environment_id"] = "The ID of the environment the service belongs to",
        ["project_id"] = "The ID of the project the service belongs to",
    };
}
