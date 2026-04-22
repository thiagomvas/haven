using Haven.Application.Common.Interfaces.Deployment;
using Haven.Domain;
using Haven.Domain.Entities;
using Haven.Domain.ValueObjects;
using Haven.Infrastructure.Persistence;

namespace Haven.Infrastructure.Deployment;

public class DeployServiceFactory : IDeployServiceFactory
{
    private readonly IEnumerable<IDeployService> _deployServices;
    private readonly HavenDbContext _db;

    public DeployServiceFactory(IEnumerable<IDeployService> deployServices, HavenDbContext db)
    {
        _deployServices = deployServices;
        _db = db;
    }

    public IDeployService? Create(Service service)
    {
        if (service.Type is ServiceType.DockerImage)
        {
            return service.SourceConfig is not DockerConfig dockerConfig
                ? null
                : _deployServices.FirstOrDefault(s => s.ServiceType == ServiceType.DockerImage);
        }

        return null;
    }
}