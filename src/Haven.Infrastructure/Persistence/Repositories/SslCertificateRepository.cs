using Haven.Application.Common.Interfaces.Repositories;
using Haven.Domain.Entities;

using Microsoft.EntityFrameworkCore;

namespace Haven.Infrastructure.Persistence.Repositories;

public class SslCertificateRepository(HavenDbContext db) : ISslCertificateRepository
{
    public Task<SslCertificate?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        db.SslCertificates.FirstOrDefaultAsync(c => c.Id == id, ct);

    public Task<List<SslCertificate>> GetAllAsync(CancellationToken ct = default) =>
        db.SslCertificates.AsNoTracking().OrderBy(c => c.Name).ToListAsync(ct);

    public Task AddAsync(SslCertificate certificate, CancellationToken ct = default)
    {
        db.SslCertificates.Add(certificate);
        return Task.CompletedTask;
    }

    public Task RemoveAsync(SslCertificate certificate, CancellationToken ct = default)
    {
        db.SslCertificates.Remove(certificate);
        return Task.CompletedTask;
    }

    public Task<int> GetAttachedDomainCountAsync(Guid certificateId, CancellationToken ct = default) =>
        db.ServiceRegistryDomains.CountAsync(d => d.SslCertificateId == certificateId, ct);
}
