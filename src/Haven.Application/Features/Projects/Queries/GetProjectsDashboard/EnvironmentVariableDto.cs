namespace Haven.Application.Features.Projects.Queries.GetProjectsDashboard;

public sealed class EnvironmentVariableDto
{
    public string Key { get; set; } = default!;
    public string Value { get; set; } = default!;
    public string Scope { get; set; } = default!;
}
