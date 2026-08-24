using Haven.Domain.Entities;

namespace Haven.Application.Common.Interfaces.Repositories;

public interface ISslCertificateRepository
{
    Task<SslCertificate?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<SslCertificate>> GetAllAsync(CancellationToken ct = default);
    Task AddAsync(SslCertificate certificate, CancellationToken ct = default);
    Task RemoveAsync(SslCertificate certificate, CancellationToken ct = default);

    /// <summary>Number of domains currently attached to this library certificate, for display/warnings.</summary>
    Task<int> GetAttachedDomainCountAsync(Guid certificateId, CancellationToken ct = default);
}
