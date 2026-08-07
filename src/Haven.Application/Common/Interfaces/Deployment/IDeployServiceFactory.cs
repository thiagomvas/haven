using Haven.Domain.Aggregates;
using Haven.Domain.Entities;

namespace Haven.Application.Common.Interfaces.Deployment;

public interface IDeployServiceFactory
{
    IDeployService? Create(Service service);
}