using Haven.Domain.Aggregates;

namespace Haven.Application.Common.Interfaces.Deployment;

public interface IDeployServiceFactory
{
    IDeployService? Create(IDeployableContainer container);
}