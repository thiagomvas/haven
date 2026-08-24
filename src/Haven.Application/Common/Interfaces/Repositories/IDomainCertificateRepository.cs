using Haven.Domain.Entities;

namespace Haven.Application.Common.Interfaces.Repositories;

public interface IDomainCertificateRepository
{
    Task<DomainCertificate?> GetByDomainIdAsync(Guid serviceRegistryDomainId, CancellationToken ct = default);
    Task AddAsync(DomainCertificate certificate, CancellationToken ct = default);
    Task RemoveByDomainIdAsync(Guid serviceRegistryDomainId, CancellationToken ct = default);
}
