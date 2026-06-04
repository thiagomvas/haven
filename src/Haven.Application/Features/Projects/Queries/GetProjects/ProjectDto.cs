namespace Haven.Application.Features.Projects.Queries.GetProjects;

public sealed class ProjectDto
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string? Alias { get; set; }
    public string? Description { get; set; }
    public int EnvironmentCount { get; set; }
    public int ServiceCount { get; set; }

    public ProjectDto()
    {

    }
    public ProjectDto(Guid id, string name, string? alias, string? description, int environmentCount, int serviceCount)
    {
        Id = id;
        Name = name;
        Alias = alias;
        Description = description;
        EnvironmentCount = environmentCount;
        ServiceCount = serviceCount;
    }
}
