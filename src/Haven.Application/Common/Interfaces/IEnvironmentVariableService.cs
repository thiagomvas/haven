using Haven.Domain.Entities;

namespace Haven.Application.Common.Interfaces;

public interface IEnvironmentVariableService
{
    Task<IEnumerable<EnvironmentVariables>> BuildVariablesForServiceAsync(Guid serviceId, CancellationToken cancellationToken);
}