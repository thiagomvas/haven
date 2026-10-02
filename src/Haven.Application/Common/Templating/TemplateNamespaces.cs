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
}