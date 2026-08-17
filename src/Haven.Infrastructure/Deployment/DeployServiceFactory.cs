using Haven.Application.Common.Interfaces.Deployment;
using Haven.Domain.Aggregates;

namespace Haven.Infrastructure.Deployment;

public class DeployServiceFactory : IDeployServiceFactory
{
    private readonly IEnumerable<IDeployService> _deployServices;

    public DeployServiceFactory(IEnumerable<IDeployService> deployServices)
    {
        _deployServices = deployServices;
    }

    public IDeployService? Create(IDeployableContainer container) =>
        _deployServices.FirstOrDefault(s => s.CanHandle(container));
}