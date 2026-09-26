using Haven.Application.Features.ServiceTemplates.Contracts;

namespace Haven.Application.Common.Interfaces.Repositories;

public interface IServiceTemplateRepository : IRepository
{
    Task<IReadOnlyList<ServiceTemplate>> GetAllAsync(CancellationToken cancellationToken);

    Task<ServiceTemplate?> GetByIdAsync(string id, CancellationToken cancellationToken);
}
