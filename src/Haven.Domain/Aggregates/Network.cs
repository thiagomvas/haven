using Haven.Domain.Entities;
using Environment = Haven.Domain.Entities.Environment;

namespace Haven.Domain.Aggregates;

public sealed class Network : AggregateRoot, ISoftDeletable
{
    public string Name { get; private set; }
    public NetworkType Type { get; private set; }
    public string? Metadata { get; private set; }
    
    public Guid? ProjectId { get; private set; }
    public Guid? EnvironmentId { get; private set; }
    
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public DateTimeOffset? DeletedAt { get; set; }

    public Project? Project { get; private set; }
    public Environment? Environment { get; private set; }
    
    public string? DockerNetworkId { get; private set; }

    public IReadOnlyList<ServiceNetwork> ServiceNetworks => _serviceNetworks.AsReadOnly();
    private List<ServiceNetwork> _serviceNetworks = [];

    private Network() {}

    public static Network Create(string name,
        NetworkType type,
        Guid? projectId = null,
        Guid? environmentId = null,
        string? metadata = null)
    {
        ValidateScope(type, projectId, environmentId);

        return new Network()
        {
            Id = Guid.NewGuid(),
            Name = name,
            Type = type,
            Metadata = metadata,
            ProjectId = projectId,
            EnvironmentId = environmentId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public static Network CreateProjectEnvironmentNetwork(Guid projectId, string projectAlias, Guid environmentId, string environmentAlias, string? metadata = null)
    {
        var name = $"{DomainConstants.NetworkBaseName}-{projectAlias}-{environmentAlias}";
        return Create(name, NetworkType.ProjectEnvironment, projectId, environmentId, metadata);
    }
    
    public static Network Reconstitute(Guid id,
        string name,
        NetworkType type,
        string? metadata,
        Guid? projectId,
        Guid? environmentId,
        DateTime createdAt,
        DateTime updatedAt,
        Project? project = null,
        Environment? environment = null,
        IEnumerable<ServiceNetwork>? serviceNetworks = null)
    {
        return new Network()
        {
            Id = id,
            Name = name,
            Type = type,
            Metadata = metadata,
            ProjectId = projectId,
            EnvironmentId = environmentId,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
            Project = project,
            Environment = environment,
            _serviceNetworks = serviceNetworks?.ToList() ?? []
        };
    }
    
    public void SetDockerNetworkId(string dockerNetworkId)
    {
        DockerNetworkId = dockerNetworkId;
        UpdatedAt = DateTime.UtcNow;
    }

    private static void ValidateScope(NetworkType type, Guid? projectId = null, Guid? environmentId = null)
    {
        switch (type)
        {
            case NetworkType.ProjectEnvironment:
                ArgumentNullException.ThrowIfNull(projectId, nameof(projectId));
                ArgumentNullException.ThrowIfNull(environmentId, nameof(environmentId));
                break;
            case NetworkType.Shared:
                if (projectId is not null) throw new ArgumentException("Shared networks cannot have a ProjectId");
                if (environmentId is not null) throw new ArgumentException("Shared networks cannot have an EnvironmentId");
                break;
            case NetworkType.External:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }
    }
}