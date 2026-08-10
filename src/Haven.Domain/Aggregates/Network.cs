using Haven.Domain.Entities;
using Haven.Domain.Enums;
using Haven.Domain.Events;

using Environment = Haven.Domain.Aggregates.Environment;

namespace Haven.Domain.Aggregates;

public sealed class Network : AggregateRoot
{
    public string Name { get; private set; }
    public NetworkType Type { get; private set; }
    public string? Metadata { get; private set; }

    public Guid? ProjectId { get; private set; }
    public Guid? EnvironmentId { get; private set; }

    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public Project? Project { get; set; }
    public Environment? Environment { get; set; }

    public string? DockerNetworkId { get; private set; }
    public string? Subnet { get; private set; }
    public string? Gateway { get; private set; }

    public IReadOnlyList<ServiceNetwork> ServiceNetworks => _serviceNetworks.AsReadOnly();
    private List<ServiceNetwork> _serviceNetworks = [];

    private Network() { }

    public static Network Create(string name,
        NetworkType type,
        Guid? projectId = null,
        Guid? environmentId = null,
        string? metadata = null)
    {
        ValidateScope(type, projectId, environmentId);

        var network = new Network()
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

        network.Raise(new NetworkCreatedEvent(network.Id, network.Name));

        return network;
    }

    public void Delete()
    {
        Raise(new NetworkDeletedEvent(Id, Name, Type));
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
        IEnumerable<ServiceNetwork>? serviceNetworks = null,
        string? dockerNetworkId = null,
        string? subnet = null,
        string? gateway = null)
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
            _serviceNetworks = serviceNetworks?.ToList() ?? [],
            DockerNetworkId = dockerNetworkId,
            Subnet = subnet,
            Gateway = gateway
        };
    }

    public void SetDockerNetworkId(string dockerNetworkId)
    {
        DockerNetworkId = dockerNetworkId;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AssignNetworkInfo(string subnet, string gateway)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subnet);
        ArgumentException.ThrowIfNullOrWhiteSpace(gateway);
        Subnet = subnet;
        Gateway = gateway;
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