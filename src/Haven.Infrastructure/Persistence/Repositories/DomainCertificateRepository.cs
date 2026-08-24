using Haven.Application.Common.Interfaces.Repositories;
using Haven.Domain.Entities;

using Microsoft.EntityFrameworkCore;

namespace Haven.Infrastructure.Persistence.Repositories;

public class DomainCertificateRepository(HavenDbContext db) : IDomainCertificateRepository
{
    public Task<DomainCertificate?> GetByDomainIdAsync(Guid serviceRegistryDomainId, CancellationToken ct = default) =>
        db.DomainCertificates.FirstOrDefaultAsync(c => c.ServiceRegistryDomainId == serviceRegistryDomainId, ct);

    public Task AddAsync(DomainCertificate certificate, CancellationToken ct = default)
    {
        db.DomainCertificates.Add(certificate);
        return Task.CompletedTask;
    }

    public async Task RemoveByDomainIdAsync(Guid serviceRegistryDomainId, CancellationToken ct = default)
    {
        var existing = await GetByDomainIdAsync(serviceRegistryDomainId, ct);
        if (existing is not null)
            db.DomainCertificates.Remove(existing);
    }
}
