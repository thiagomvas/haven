using Haven.Domain.Aggregates;

namespace Haven.Application.Features.Services.Graph;

public sealed record ServiceNode(string Name, Service Service, List<ServiceNode> Dependencies)
{
    public static ServiceNode FromService(Service service, List<ServiceNode> dependencies)
    {
        return new ServiceNode(service.Name, service, dependencies);
    }
    public override string ToString()
    {
        return Name;
    }
}