namespace Haven.Application.Configuration;

public class ManifestsOptions
{
    public const string SectionName = "Manifests";
    public string ManifestsPath { get; set; } = "manifests";
    public bool IncludeEnvValuesOnExample { get; set; } = true;

    public ManifestsOptions()
    {

    }
}
